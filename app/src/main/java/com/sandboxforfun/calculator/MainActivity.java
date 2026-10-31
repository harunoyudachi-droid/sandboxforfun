package com.sandboxforfun.calculator;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.graphics.Insets;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.RippleDrawable;
import android.content.res.ColorStateList;
import android.os.Build;
import android.os.Bundle;
import android.text.InputType;
import android.view.Gravity;
import android.view.View;
import android.view.WindowInsets;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import org.json.JSONArray;
import java.util.ArrayList;
import java.util.Locale;

public class MainActivity extends Activity {
    private static final int BACKGROUND = Color.rgb(17, 23, 20);
    private static final int FOREGROUND = Color.rgb(235, 242, 231);
    private static final int ACCENT = Color.rgb(184, 238, 132);
    private EditText expression;
    private TextView result;
    private Button mode;
    private boolean degrees = true;
    private double answer = 0;
    private final ArrayList<String> history = new ArrayList<>();

    @Override public void onCreate(Bundle saved) {
        super.onCreate(saved);
        SharedPreferences preferences = getPreferences(MODE_PRIVATE);
        degrees = preferences.getBoolean("degrees", true);
        answer = Double.longBitsToDouble(preferences.getLong("answer", Double.doubleToLongBits(0)));
        try {
            JSONArray items = new JSONArray(preferences.getString("history", "[]"));
            for (int i = 0; i < Math.min(20, items.length()); i++) history.add(items.getString(i));
        } catch (Exception ignored) { history.clear(); }
        String draft = preferences.getString("expression", "");
        String displayedResult = preferences.getString("result", "0");
        if (saved != null) {
            degrees = saved.getBoolean("degrees", degrees);
            answer = saved.getDouble("answer", answer);
            draft = saved.getString("expression", draft);
            displayedResult = saved.getString("result", displayedResult);
        }

        LinearLayout outer = new LinearLayout(this);
        outer.setOrientation(LinearLayout.VERTICAL);
        outer.setBackgroundColor(BACKGROUND);
        // Respect navigation, status bars, and cutouts on Android 15 too.
        outer.setOnApplyWindowInsetsListener((view, insets) -> {
            if (Build.VERSION.SDK_INT >= 30) {
                Insets bars = insets.getInsets(WindowInsets.Type.systemBars() | WindowInsets.Type.displayCutout());
                view.setPadding(bars.left, bars.top, bars.right, bars.bottom);
            } else {
                view.setPadding(insets.getSystemWindowInsetLeft(), insets.getSystemWindowInsetTop(),
                        insets.getSystemWindowInsetRight(), insets.getSystemWindowInsetBottom());
            }
            return insets;
        });
        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        outer.addView(scroll, new LinearLayout.LayoutParams(-1, -1));
        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        content.setPadding(dp(16), dp(18), dp(16), dp(20));
        scroll.addView(content, new ScrollView.LayoutParams(-1, -2));

        TextView eyebrow = label("SANDBOX  /  SCIENTIFIC", 12, ACCENT);
        eyebrow.setLetterSpacing(0.15f);
        content.addView(eyebrow);
        TextView title = label("Инженерный\nкалькулятор", 28, FOREGROUND);
        title.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        title.setPadding(0, dp(8), 0, dp(12));
        content.addView(title);

        LinearLayout toolbar = row(content);
        mode = button(degrees ? "DEG" : "RAD", "Переключить градусы и радианы", false, true);
        mode.setOnClickListener(v -> {
            degrees = !degrees;
            mode.setText(degrees ? "DEG" : "RAD");
            result.setText(degrees ? "Режим: градусы" : "Режим: радианы");
        });
        toolbar.addView(mode, cell());
        Button historyButton = button("История", "Показать историю вычислений", false, false);
        historyButton.setOnClickListener(v -> showHistory());
        toolbar.addView(historyButton, cell());

        expression = new EditText(this);
        expression.setId(View.generateViewId());
        expression.setTextColor(FOREGROUND);
        expression.setTextSize(26);
        expression.setSingleLine(true);
        expression.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS);
        expression.setShowSoftInputOnFocus(false);
        expression.setHint("Введите выражение");
        expression.setHintTextColor(Color.rgb(135, 151, 140));
        expression.setContentDescription("Математическое выражение");
        expression.setGravity(Gravity.END | Gravity.CENTER_VERTICAL);
        expression.setPadding(dp(12), dp(18), dp(12), dp(18));
        expression.setText(draft);
        expression.setSelection(expression.length());
        content.addView(expression, new LinearLayout.LayoutParams(-1, -2));

        result = label(displayedResult, 24, ACCENT);
        result.setTextIsSelectable(true);
        result.setGravity(Gravity.END);
        result.setPadding(dp(12), dp(6), dp(12), dp(18));
        result.setAccessibilityLiveRegion(View.ACCESSIBILITY_LIVE_REGION_POLITE);
        content.addView(result, new LinearLayout.LayoutParams(-1, -2));

        String[][] keys = {
                {"sin", "cos", "tan", "(", ")"},
                {"asin", "acos", "atan", "ln", "log"},
                {"√", "x²", "xʸ", "!", "%"},
                {"π", "e", "abs", "exp", "Ans"},
                {"AC", "7", "8", "9", "÷"},
                {"⌫", "4", "5", "6", "×"},
                {"±", "1", "2", "3", "−"},
                {"0", ".", "E", "=", "+"}
        };
        for (String[] keyRow : keys) {
            LinearLayout line = row(content);
            for (String key : keyRow) {
                boolean operator = "+−×÷=".contains(key);
                String description = description(key);
                Button b = button(key, description, key.equals("="), operator);
                b.setOnClickListener(v -> press(key));
                line.addView(b, cell());
            }
        }
        TextView help = label("DEG — градусы · RAD — радианы\n% = ÷100 · E — экспонента числа · Ans — ответ\nУмножение вводите явно: 2 × π", 12, Color.rgb(155, 172, 158));
        help.setPadding(dp(4), dp(16), dp(4), 0);
        content.addView(help);
        setContentView(outer);
        outer.requestApplyInsets();
    }

    private void press(String key) {
        switch (key) {
            case "AC": expression.setText(""); result.setText("0"); break;
            case "⌫":
                int start = Math.max(0, expression.getSelectionStart());
                int end = Math.max(0, expression.getSelectionEnd());
                int left = Math.min(start, end), right = Math.max(start, end);
                if (left != right) expression.getText().delete(left, right);
                else if (left > 0) expression.getText().delete(left - 1, left);
                break;
            case "=": evaluate(); break;
            case "±":
                if (expression.length() > 0) {
                    expression.setText("-(" + expression.getText() + ")");
                    expression.setSelection(expression.length());
                } else insert("-");
                break;
            case "√": insert("sqrt("); break;
            case "x²": insert("^2"); break;
            case "xʸ": insert("^"); break;
            case "sin": case "cos": case "tan": case "asin": case "acos":
            case "atan": case "ln": case "log": case "abs": case "exp":
                insert(key + "("); break;
            default: insert(key);
        }
    }

    private void insert(String text) {
        int start = Math.max(0, expression.getSelectionStart());
        int end = Math.max(0, expression.getSelectionEnd());
        if (expression.length() - Math.abs(start - end) + text.length() > 512) {
            result.setText("Выражение слишком длинное"); return;
        }
        expression.getText().replace(Math.min(start, end), Math.max(start, end), text);
        expression.setSelection(Math.min(start, end) + text.length());
    }

    private void evaluate() {
        String text = expression.getText().toString();
        try {
            answer = ExpressionEngine.evaluate(text, degrees, answer);
            String value = format(answer);
            result.setText(value);
            history.add(0, text + "  [" + (degrees ? "DEG" : "RAD") + "]\n= " + value);
            if (history.size() > 20) history.remove(history.size() - 1);
        } catch (IllegalArgumentException ex) { result.setText(ex.getMessage()); }
    }

    private void showHistory() {
        if (history.isEmpty()) {
            new AlertDialog.Builder(this).setTitle("История").setMessage("Здесь появятся вычисления")
                    .setPositiveButton("Понятно", null).show();
            return;
        }
        new AlertDialog.Builder(this).setTitle("Последние вычисления")
                .setItems(history.toArray(new String[0]), (dialog, which) -> {
                    String entry = history.get(which);
                    expression.setText(entry.substring(0, entry.lastIndexOf("  [")));
                    expression.setSelection(expression.length());
                    degrees = entry.contains("  [DEG]");
                    mode.setText(degrees ? "DEG" : "RAD");
                    result.setText(entry.substring(entry.lastIndexOf("\n= ") + 3));
                }).setNegativeButton("Закрыть", null).show();
    }

    @Override protected void onSaveInstanceState(Bundle state) {
        super.onSaveInstanceState(state);
        state.putString("expression", expression.getText().toString());
        state.putString("result", result.getText().toString());
        state.putBoolean("degrees", degrees);
        state.putDouble("answer", answer);
    }

    @Override protected void onPause() {
        super.onPause();
        getPreferences(MODE_PRIVATE).edit().putBoolean("degrees", degrees)
                .putLong("answer", Double.doubleToLongBits(answer))
                .putString("expression", expression.getText().toString())
                .putString("result", result.getText().toString())
                .putString("history", new JSONArray(history).toString()).apply();
    }

    private static String format(double value) {
        if (value == 0) return "0";
        return String.format(Locale.US, "%.12g", value).replaceAll("(\\.\\d*?)0+(e|$)", "$1$2")
                .replaceAll("\\.(e|$)", "$1");
    }
    private TextView label(String text, int size, int color) {
        TextView view = new TextView(this);
        view.setText(text); view.setTextSize(size); view.setTextColor(color);
        return view;
    }
    private LinearLayout row(LinearLayout parent) {
        LinearLayout line = new LinearLayout(this);
        line.setOrientation(LinearLayout.HORIZONTAL);
        parent.addView(line, new LinearLayout.LayoutParams(-1, -2));
        return line;
    }
    private LinearLayout.LayoutParams cell() {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, dp(52), 1);
        params.setMargins(dp(3), dp(3), dp(3), dp(3));
        return params;
    }
    private Button button(String text, String description, boolean primary, boolean operation) {
        Button b = new Button(this);
        b.setText(text); b.setContentDescription(description); b.setAllCaps(false);
        b.setTextSize(16); b.setMinWidth(0); b.setMinimumWidth(0);
        b.setPadding(dp(2), 0, dp(2), 0);
        b.setTextColor(primary ? BACKGROUND : operation ? ACCENT : FOREGROUND);
        GradientDrawable shape = new GradientDrawable();
        shape.setColor(primary ? ACCENT : operation ? Color.rgb(49, 65, 47) : Color.rgb(32, 43, 36));
        shape.setCornerRadius(dp(16));
        b.setBackground(new RippleDrawable(ColorStateList.valueOf(0x33ffffff), shape, null));
        return b;
    }
    private String description(String key) {
        switch (key) {
            case "AC": return "Очистить выражение";
            case "⌫": return "Удалить символ или выделение";
            case "√": return "Квадратный корень";
            case "x²": return "Квадрат числа";
            case "xʸ": return "Возвести в степень";
            case "!": return "Факториал";
            case "±": return "Изменить знак всего выражения";
            case "Ans": return "Последний ответ";
            case "E": return "Экспоненциальная запись числа";
            case "=": return "Вычислить";
            default: return key;
        }
    }
    private int dp(int value) { return Math.round(value * getResources().getDisplayMetrics().density); }
}
