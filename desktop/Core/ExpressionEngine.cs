using System.Globalization;

namespace SandboxCalculator;

/// <summary>Parses mathematical expressions without executing code.</summary>
public sealed class ExpressionEngine
{
    private readonly string source;
    private readonly bool degrees;
    private readonly double answer;
    private int position;
    private int depth;

    private ExpressionEngine(string text, bool degrees, double answer)
    {
        source = text.Replace('×', '*').Replace('÷', '/').Replace('−', '-')
            .Replace(',', '.').Replace("π", "pi").ToLowerInvariant();
        this.degrees = degrees;
        this.answer = answer;
    }

    public static double Evaluate(string text, bool degrees = true, double answer = 0)
    {
        if (string.IsNullOrWhiteSpace(text)) throw Error("Введите выражение");
        if (text.Length > 512) throw Error("Выражение слишком длинное");
        var parser = new ExpressionEngine(text, degrees, answer);
        var result = parser.Expression();
        parser.Spaces();
        if (parser.position != parser.source.Length)
            throw Error($"Лишний символ в позиции {parser.position + 1}");
        return Finite(result);
    }

    private double Expression()
    {
        var value = Term();
        while (true)
        {
            if (Eat('+')) value = Finite(value + Term());
            else if (Eat('-')) value = Finite(value - Term());
            else return value;
        }
    }

    private double Term()
    {
        var value = Unary();
        while (true)
        {
            if (Eat('*')) value = Finite(value * Unary());
            else if (Eat('/'))
            {
                var divisor = Unary();
                if (divisor == 0) throw Error("Делить на ноль нельзя");
                value = Finite(value / divisor);
            }
            else return value;
        }
    }

    // -2^2 = -4, 2^-2 = 0.25; exponentiation is right-associative.
    private double Unary()
    {
        if (++depth > 64) throw Error("Слишком много вложенных операций");
        try
        {
            if (Eat('+')) return Unary();
            if (Eat('-')) return -Unary();
            var value = Postfix();
            if (Eat('^')) value = Finite(Math.Pow(value, Unary()));
            return value;
        }
        finally { depth--; }
    }

    private double Postfix()
    {
        var value = Primary();
        while (true)
        {
            if (Eat('!'))
            {
                if (value < 0 || value != Math.Truncate(value) || value > 170)
                    throw Error("Факториал: целое число от 0 до 170");
                double result = 1;
                for (var i = 2; i <= (int)value; i++) result *= i;
                value = result;
            }
            else if (Eat('%')) value /= 100;
            else return value;
        }
    }

    private double Primary()
    {
        if (Eat('('))
        {
            var value = Expression();
            if (!Eat(')')) throw Error("Не хватает закрывающей скобки");
            return value;
        }
        Spaces();
        var start = position;
        while (position < source.Length && char.IsLetter(source[position])) position++;
        if (position > start)
        {
            var name = source[start..position];
            if (name == "pi") return Math.PI;
            if (name == "e") return Math.E;
            if (name == "ans") return Finite(answer);
            if (!Eat('(')) throw Error("После функции нужна скобка (");
            var value = Expression();
            if (!Eat(')')) throw Error("Не хватает закрывающей скобки");
            return Function(name, value);
        }
        start = position;
        var digit = false;
        while (position < source.Length && char.IsAsciiDigit(source[position])) { digit = true; position++; }
        if (position < source.Length && source[position] == '.')
        {
            position++;
            while (position < source.Length && char.IsAsciiDigit(source[position])) { digit = true; position++; }
        }
        if (!digit) throw Error($"Ожидается число в позиции {position + 1}");
        if (position < source.Length && source[position] == 'e')
        {
            position++;
            if (position < source.Length && source[position] is '+' or '-') position++;
            var exponent = position;
            while (position < source.Length && char.IsAsciiDigit(source[position])) position++;
            if (exponent == position) throw Error("Неверная экспонента числа");
        }
        if (!double.TryParse(source[start..position], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            throw Error("Неверное число");
        return Finite(number);
    }

    private double Function(string name, double value)
    {
        var radians = degrees ? value * (Math.PI / 180) : value;
        var result = name switch
        {
            "sin" => Math.Sin(radians),
            "cos" => Math.Cos(radians),
            "tan" => Math.Abs(Math.Cos(radians)) < 1e-15
                ? throw Error("Тангенс не определён") : Math.Tan(radians),
            "asin" => FromRadians(Math.Asin(value)),
            "acos" => FromRadians(Math.Acos(value)),
            "atan" => FromRadians(Math.Atan(value)),
            "sqrt" => Math.Sqrt(value),
            "ln" => Math.Log(value),
            "log" => Math.Log10(value),
            "abs" => Math.Abs(value),
            "exp" => Math.Exp(value),
            _ => throw Error("Неизвестная функция: " + name)
        };
        return Finite(result);
    }

    private double FromRadians(double value) => degrees ? value * (180 / Math.PI) : value;
    private bool Eat(char c)
    {
        Spaces();
        if (position < source.Length && source[position] == c) { position++; return true; }
        return false;
    }
    private void Spaces()
    {
        while (position < source.Length && char.IsWhiteSpace(source[position])) position++;
    }
    private static double Finite(double value)
    {
        if (double.IsNaN(value)) throw Error("Функция не определена для этого значения");
        if (double.IsInfinity(value)) throw Error("Результат выходит за диапазон чисел");
        return value;
    }
    private static ArgumentException Error(string message) => new(message);
}
