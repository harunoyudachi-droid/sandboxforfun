package com.sandboxforfun.calculator;

import org.junit.Test;
import static org.junit.Assert.*;

public class ExpressionEngineTest {
    private void check(String expression, double expected) {
        assertEquals(expression, expected, ExpressionEngine.evaluate(expression, true, 7), 1e-10);
    }
    private void invalid(String expression) {
        assertThrows(expression, IllegalArgumentException.class,
                () -> ExpressionEngine.evaluate(expression, true, 0));
    }
    @Test public void arithmeticAndPrecedence() {
        check("2+3*4", 14); check("(2+3)*4", 20); check("7/2-1", 2.5);
        check("-2^2", -4); check("(-2)^2", 4); check("2^-2", 0.25);
        check("2^3^2", 512); check("2--3", 5); check(".5+1,5", 2);
        check(" 2 × 3 − 8 ÷ 4 ", 4);
    }
    @Test public void scientificFunctions() {
        check("sin(30)", 0.5); check("cos(60)", 0.5); check("tan(45)", 1);
        check("asin(0.5)", 30); check("acos(0.5)", 60); check("atan(1)", 45);
        check("sqrt(81)+log(100)+ln(e)", 12);
        check("abs(-4)+exp(0)", 5); check("sin(cos(0)*30)", 0.5);
        assertEquals(1, ExpressionEngine.evaluate("sin(pi/2)", false, 0), 1e-12);
        assertEquals(Math.PI/2, ExpressionEngine.evaluate("asin(1)", false, 0), 1e-12);
    }
    @Test public void constantsPercentFactorialAndAnswer() {
        check("π", Math.PI); check("e", Math.E); check("Ans*2", 14);
        check("0!+5!", 121); check("3!^2", 36); check("200*10%", 20);
        check("100+10%", 100.1); check("1.5E3+2e-2", 1500.02);
    }
    @Test public void invalidInputsAndDomains() {
        for (String expression : new String[]{"", " ", "1/0", "0/0", "sqrt(-1)", "ln(0)",
                "log(-1)", "asin(2)", "acos(-2)", "tan(90)", "(-1)!", "2.5!", "171!",
                "1e309", "exp(1000)", "(-2)^0.5", "2+", "(2+3", "2+3)", "2(3)",
                "2pi", "sin30", "bad(1)", "1.2.3", "1e", "1e+", "NaN", "1;quit()"}) invalid(expression);
    }
    @Test public void limitsPreventStackOverflow() {
        invalid("(".repeat(100) + "1" + ")".repeat(100));
        invalid("-".repeat(100) + "1");
        invalid("1+".repeat(300) + "1");
    }
    @Test public void failedEvaluationDoesNotPoisonNextCalculation() {
        invalid("1/0"); check("2+2", 4);
    }
}
