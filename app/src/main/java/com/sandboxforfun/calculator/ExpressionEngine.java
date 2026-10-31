package com.sandboxforfun.calculator;

import java.util.Locale;

/** Recursive descent parser; never evaluates executable code. */
public final class ExpressionEngine {
    private final String source;
    private final boolean degrees;
    private final double answer;
    private int position;
    private int depth;

    private ExpressionEngine(String source, boolean degrees, double answer) {
        this.source = source.replace('×', '*').replace('÷', '/').replace('−', '-')
                .replace(',', '.').replace("π", "pi").toLowerCase(Locale.ROOT);
        this.degrees = degrees;
        this.answer = answer;
    }

    public static double evaluate(String source, boolean degrees, double answer) {
        if (source == null || source.trim().isEmpty()) throw error("Введите выражение");
        if (source.length() > 512) throw error("Выражение слишком длинное");
        ExpressionEngine parser = new ExpressionEngine(source, degrees, answer);
        double result = parser.expression();
        parser.spaces();
        if (parser.position != parser.source.length()) {
            throw error("Лишний символ в позиции " + (parser.position + 1));
        }
        return finite(result);
    }

    private double expression() {
        double value = term();
        while (true) {
            if (eat('+')) value = finite(value + term());
            else if (eat('-')) value = finite(value - term());
            else return value;
        }
    }

    private double term() {
        double value = unary();
        while (true) {
            if (eat('*')) value = finite(value * unary());
            else if (eat('/')) {
                double divisor = unary();
                if (divisor == 0) throw error("Делить на ноль нельзя");
                value = finite(value / divisor);
            } else return value;
        }
    }

    // Unary minus binds below exponentiation: -2^2 = -4, 2^-2 = 0.25.
    private double unary() {
        enter();
        try {
            if (eat('+')) return unary();
            if (eat('-')) return -unary();
            return power();
        } finally { depth--; }
    }

    // Exponentiation associates right: 2^3^2 = 2^(3^2).
    private double power() {
        double value = postfix();
        if (eat('^')) value = finite(Math.pow(value, unary()));
        return value;
    }

    private double postfix() {
        double value = primary();
        while (true) {
            if (eat('!')) {
                if (value < 0 || value != Math.rint(value) || value > 170) {
                    throw error("Факториал: целое число от 0 до 170");
                }
                double result = 1;
                for (int i = 2; i <= (int) value; i++) result *= i;
                value = result;
            } else if (eat('%')) value /= 100;
            else return value;
        }
    }

    private double primary() {
        if (eat('(')) {
            double value = expression();
            if (!eat(')')) throw error("Не хватает закрывающей скобки");
            return value;
        }
        spaces();
        int start = position;
        while (position < source.length() && Character.isLetter(source.charAt(position))) position++;
        if (position > start) {
            String name = source.substring(start, position);
            if (name.equals("pi")) return Math.PI;
            if (name.equals("e")) return Math.E;
            if (name.equals("ans")) return finite(answer);
            if (!eat('(')) throw error("После функции нужна скобка (");
            double value = expression();
            if (!eat(')')) throw error("Не хватает закрывающей скобки");
            return function(name, value);
        }
        start = position;
        boolean digit = false;
        while (position < source.length() && Character.isDigit(source.charAt(position))) {
            digit = true; position++;
        }
        if (position < source.length() && source.charAt(position) == '.') {
            position++;
            while (position < source.length() && Character.isDigit(source.charAt(position))) {
                digit = true; position++;
            }
        }
        if (!digit) throw error("Ожидается число в позиции " + (position + 1));
        if (position < source.length() && source.charAt(position) == 'e') {
            position++;
            if (position < source.length() && (source.charAt(position) == '+' || source.charAt(position) == '-')) position++;
            int exponent = position;
            while (position < source.length() && Character.isDigit(source.charAt(position))) position++;
            if (exponent == position) throw error("Неверная экспонента числа");
        }
        try { return finite(Double.parseDouble(source.substring(start, position))); }
        catch (NumberFormatException ex) { throw error("Неверное число"); }
    }

    private double function(String name, double value) {
        double radians = degrees ? Math.toRadians(value) : value;
        double result;
        switch (name) {
            case "sin": result = Math.sin(radians); break;
            case "cos": result = Math.cos(radians); break;
            case "tan":
                if (Math.abs(Math.cos(radians)) < 1e-15) throw error("Тангенс не определён");
                result = Math.tan(radians); break;
            case "asin": result = Math.asin(value); return finite(degrees ? Math.toDegrees(result) : result);
            case "acos": result = Math.acos(value); return finite(degrees ? Math.toDegrees(result) : result);
            case "atan": result = Math.atan(value); return finite(degrees ? Math.toDegrees(result) : result);
            case "sqrt": result = Math.sqrt(value); break;
            case "ln": result = Math.log(value); break;
            case "log": result = Math.log10(value); break;
            case "abs": result = Math.abs(value); break;
            case "exp": result = Math.exp(value); break;
            default: throw error("Неизвестная функция: " + name);
        }
        return finite(result);
    }

    private boolean eat(char c) {
        spaces();
        if (position < source.length() && source.charAt(position) == c) { position++; return true; }
        return false;
    }
    private void spaces() {
        while (position < source.length() && Character.isWhitespace(source.charAt(position))) position++;
    }
    private void enter() {
        if (++depth > 64) throw error("Слишком много вложенных операций");
    }
    private static double finite(double value) {
        if (Double.isNaN(value)) throw error("Функция не определена для этого значения");
        if (Double.isInfinite(value)) throw error("Результат выходит за диапазон чисел");
        return value;
    }
    private static IllegalArgumentException error(String message) {
        return new IllegalArgumentException(message);
    }
}
