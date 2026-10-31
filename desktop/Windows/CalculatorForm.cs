using System.Runtime.InteropServices;

namespace SandboxCalculator;

public sealed class CalculatorForm : Form
{
    private static readonly Color Background = Color.FromArgb(17, 23, 20);
    private static readonly Color Surface = Color.FromArgb(32, 43, 36);
    private static readonly Color Accent = Color.FromArgb(184, 238, 132);
    private static readonly Color Ink = Color.FromArgb(235, 242, 231);
    private readonly CalculatorSession session;
    private readonly string? statePath;
    private readonly TextBox expression = new();
    private readonly TextBox result = new();
    private readonly ListBox history = new();
    private readonly Button mode = new();
    private readonly Label status = new();
    private readonly Dictionary<string, Button> keys = [];

    public CalculatorForm(bool persist = true)
    {
        statePath = persist ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SandboxForFun", "EngineeringCalculator", "state.json") : null;
        session = statePath is null ? new() : SessionStore.Load(statePath);
        Text = "Инженерный калькулятор · Sandbox";
        BackColor = Background;
        ForeColor = Ink;
        Font = new Font("Segoe UI", 11);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1060, 730);
        MinimumSize = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = new Padding(0, 0, 22, 0) };
        foreach (var height in new float[] { 28, 56, 60, 64 }) left.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(left, 0, 0);
        left.Controls.Add(Label("SANDBOX  /  SCIENTIFIC", 11, Accent), 0, 0);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        header.Controls.Add(Label("Инженерный калькулятор", 21, Ink), 0, 0);
        Style(mode, "DEG", true);
        mode.AccessibleName = "Переключить градусы и радианы";
        mode.Click += (_, _) => { session.Degrees = !session.Degrees; RefreshMode(); status.Text = session.Degrees ? "Градусы" : "Радианы"; expression.Focus(); };
        header.Controls.Add(mode, 1, 0);
        left.Controls.Add(header, 0, 1);

        expression.Dock = DockStyle.Fill;
        expression.BackColor = Surface;
        expression.ForeColor = Ink;
        expression.BorderStyle = BorderStyle.FixedSingle;
        expression.Font = new Font("Segoe UI", 23);
        expression.TextAlign = HorizontalAlignment.Right;
        expression.MaxLength = 512;
        expression.AccessibleName = "Математическое выражение";
        expression.PlaceholderText = "Введите выражение";
        expression.Margin = new Padding(3, 8, 3, 3);
        expression.Text = session.Expression;
        left.Controls.Add(expression, 0, 2);

        result.Dock = DockStyle.Fill;
        result.ReadOnly = true;
        result.BorderStyle = BorderStyle.None;
        result.BackColor = Background;
        result.ForeColor = Accent;
        result.Font = new Font("Segoe UI", 26, FontStyle.Bold);
        result.TextAlign = HorizontalAlignment.Right;
        result.AccessibleName = "Результат вычисления";
        result.Text = session.Display;
        result.Margin = new Padding(3, 10, 3, 3);
        left.Controls.Add(result, 0, 3);

        var keypad = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 8, Margin = Padding.Empty };
        for (var i = 0; i < 5; i++) keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        for (var i = 0; i < 8; i++) keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5f));
        string[][] rows = [
            ["sin", "cos", "tan", "(", ")"],
            ["asin", "acos", "atan", "ln", "log"],
            ["√", "x²", "xʸ", "!", "%"],
            ["π", "e", "abs", "exp", "Ans"],
            ["AC", "7", "8", "9", "÷"],
            ["⌫", "4", "5", "6", "×"],
            ["±", "1", "2", "3", "−"],
            ["0", ".", "E", "=", "+"]
        ];
        for (var row = 0; row < rows.Length; row++)
        for (var col = 0; col < rows[row].Length; col++)
        {
            var key = rows[row][col];
            var button = new Button();
            Style(button, key, "+−×÷=".Contains(key));
            if (key == "=") { button.BackColor = Accent; button.ForeColor = Background; }
            button.AccessibleName = Description(key);
            button.Click += (_, _) => Press(key);
            keys.Add(key, button);
            keypad.Controls.Add(button, col, row);
        }
        left.Controls.Add(keypad, 0, 4);
        left.Controls.Add(Label("Enter — вычислить   ·   Esc — очистить   ·   F2 — DEG/RAD\n% = ÷100   ·   E — экспонента   ·   умножение: 2 × π", 9, Color.Silver), 0, 5);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.Controls.Add(right, 1, 0);
        right.Controls.Add(Label("История", 20, Ink), 0, 0);
        right.Controls.Add(Label("Последние 20 вычислений.\nДвойной щелчок — вернуть выражение.", 9, Color.Silver), 0, 1);
        history.Dock = DockStyle.Fill;
        history.BackColor = Surface;
        history.ForeColor = Ink;
        history.BorderStyle = BorderStyle.None;
        history.Font = new Font("Segoe UI", 11);
        history.HorizontalScrollbar = true;
        history.IntegralHeight = false;
        history.ItemHeight = 38;
        history.DrawMode = DrawMode.OwnerDrawFixed;
        history.DrawItem += DrawHistoryItem;
        history.AccessibleName = "История вычислений";
        history.DoubleClick += (_, _) => RecallSelected();
        history.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { RecallSelected(); e.Handled = e.SuppressKeyPress = true; } };
        right.Controls.Add(history, 0, 2);
        var copy = new Button();
        Style(copy, "Копировать результат", false);
        copy.Click += (_, _) => CopyResult();
        right.Controls.Add(copy, 0, 3);
        status.Dock = DockStyle.Fill;
        status.ForeColor = Color.Silver;
        status.Font = new Font("Segoe UI", 9);
        status.Text = "Ctrl+Shift+C — копировать ответ\nДанные сохраняются на этом компьютере";
        right.Controls.Add(status, 0, 4);
        RefreshHistory();
        RefreshMode();
        Shown += (_, _) => { expression.Focus(); expression.SelectionStart = expression.TextLength; };
        FormClosing += (_, _) => {
            session.Expression = expression.Text;
            if (statePath is not null && !SessionStore.Save(statePath, session))
                MessageBox.Show(this, "Не удалось сохранить историю. Проверьте доступ к папке профиля.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
    }

    private static Label Label(string text, float size, Color color) => new()
    {
        Text = text, Font = new Font("Segoe UI", size), ForeColor = color,
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
    };
    private static void Style(Button button, string text, bool operation)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = operation ? Color.FromArgb(49, 65, 47) : Surface;
        button.ForeColor = operation ? Accent : Ink;
        button.Font = new Font("Segoe UI", 12);
        button.Margin = new Padding(3);
        button.UseVisualStyleBackColor = false;
    }

    private void Press(string key)
    {
        switch (key)
        {
            case "=": Calculate(); break;
            case "AC": expression.Clear(); session.Expression = ""; session.Display = "0"; result.Text = "0"; status.Text = "Выражение очищено"; break;
            case "⌫":
                if (expression.SelectionLength > 0) expression.SelectedText = "";
                else if (expression.SelectionStart > 0) {
                    var start = expression.SelectionStart;
                    expression.Select(start - 1, 1); expression.SelectedText = "";
                }
                break;
            case "±":
                if (expression.TextLength == 0) Insert("-");
                else if (expression.TextLength <= 509) { expression.Text = "-(" + expression.Text + ")"; expression.SelectionStart = expression.TextLength; }
                else status.Text = "Выражение слишком длинное";
                break;
            case "√": Insert("sqrt("); break;
            case "x²": Insert("^2"); break;
            case "xʸ": Insert("^"); break;
            case "sin": case "cos": case "tan": case "asin": case "acos": case "atan":
            case "ln": case "log": case "abs": case "exp": Insert(key + "("); break;
            default: Insert(key); break;
        }
        expression.Focus();
    }
    private void Insert(string text)
    {
        if (expression.TextLength - expression.SelectionLength + text.Length > 512) { status.Text = "Выражение слишком длинное"; return; }
        expression.SelectedText = text;
    }
    private void Calculate()
    {
        try
        {
            session.Calculate(expression.Text);
            result.Text = session.Display;
            status.Text = session.Degrees ? "Вычислено в градусах (DEG)" : "Вычислено в радианах (RAD)";
            RefreshHistory();
        }
        catch (ArgumentException ex) { status.Text = ex.Message; result.Text = "Ошибка"; }
    }
    private void CopyResult()
    {
        try { Clipboard.SetText(session.Display); status.Text = "Последний успешный ответ скопирован"; }
        catch (ExternalException) { status.Text = "Буфер обмена занят. Попробуйте ещё раз."; }
    }
    private void RefreshMode() => mode.Text = session.Degrees ? "DEG" : "RAD";
    private void RefreshHistory()
    {
        history.BeginUpdate(); history.Items.Clear();
        foreach (var entry in session.History) history.Items.Add(entry);
        history.HorizontalExtent = session.History.Count == 0 ? 0 : session.History.Max(entry => TextRenderer.MeasureText(entry.ToString(), history.Font).Width) + 16;
        history.EndUpdate();
    }
    private void RecallSelected()
    {
        if (history.SelectedItem is not HistoryEntry entry) return;
        session.Recall(entry); expression.Text = session.Expression; result.Text = session.Display;
        RefreshMode(); expression.Focus(); expression.SelectionStart = expression.TextLength;
        status.Text = "Выражение восстановлено из истории";
    }
    private void DrawHistoryItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index >= 0) TextRenderer.DrawText(e.Graphics, history.Items[e.Index].ToString(), history.Font,
            Rectangle.Inflate(e.Bounds, -6, 0), (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : Ink,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        e.DrawFocusRectangle();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Enter && !history.Focused) { Calculate(); return true; }
        if (keyData == Keys.Escape || keyData == (Keys.Control | Keys.L)) { Press("AC"); return true; }
        if (keyData == Keys.F2) { mode.PerformClick(); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.C)) { CopyResult(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    private static string Description(string key) => key switch {
        "AC" => "Очистить выражение", "⌫" => "Удалить символ или выделение", "√" => "Квадратный корень",
        "x²" => "Квадрат числа", "xʸ" => "Возвести в степень", "!" => "Факториал",
        "±" => "Изменить знак выражения", "Ans" => "Последний ответ", "=" => "Вычислить", _ => key
    };

    // Exercised by the CI smoke test on a real Windows desktop session.
    internal void ClickKey(string key) => keys[key].PerformClick();
    internal void SetExpression(string text) { expression.Text = text; expression.SelectionStart = expression.TextLength; }
    internal void SelectExpression(int start, int length) => expression.Select(start, length);
    internal string ExpressionText => expression.Text;
    internal string ResultText => result.Text;
    internal int HistoryCount => history.Items.Count;
    internal void ToggleMode() => mode.PerformClick();
    internal void RecallFirst() { history.SelectedIndex = 0; RecallSelected(); }
    internal bool Degrees => session.Degrees;
}
