namespace FateDice;

public sealed class FateForm : Form
{
    private readonly NumericUpDown[] modifiers = new NumericUpDown[6];
    private readonly Button[] buttons = new Button[6];
    private readonly Label[,] dice = new Label[6, 4];
    private readonly Label[] totals = new Label[6];
    private readonly Label details = new();
    private readonly CheckResult?[] results = new CheckResult?[6];
    private readonly Func<int>? nextFace;
    private readonly string? statePath;

    public FateForm(bool persist = true, Func<int>? nextFace = null)
    {
        this.nextFace = nextFace;
        statePath = persist ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SandboxForFun", "FateDice", "modifiers.json") : null;
        var saved = statePath is null ? new int[6] : ModifierStore.Load(statePath);
        Text = "Fate — проверки подходов";
        Font = new Font("Segoe UI", 11);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;
        ForeColor = Color.FromArgb(30, 30, 30);
        ClientSize = new Size(900, 660);
        MinimumSize = new Size(850, 620);
        StartPosition = FormStartPosition.CenterScreen;

        var outer = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 4 };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        Controls.Add(outer);
        outer.Controls.Add(MakeLabel("Fate · проверка подхода", 23, FontStyle.Bold), 0, 0);
        outer.Controls.Add(MakeLabel("Введите модификатор и нажмите «Бросить» в нужной строке.", 11), 0, 1);

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 7, Margin = Padding.Empty };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 216));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        for (var row = 1; row <= 6; row++) table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));
        outer.Controls.Add(table, 0, 2);
        string[] headings = ["Подход", "Модификатор", "", "Четыре кубика", "Итог"];
        for (var col = 0; col < headings.Length; col++) table.Controls.Add(MakeLabel(headings[col], 10, FontStyle.Bold), col, 0);

        for (var i = 0; i < 6; i++)
        {
            var index = i;
            var name = FateRoller.Approaches[i];
            table.Controls.Add(MakeLabel(name, 12), 0, i + 1);
            var number = new NumericUpDown {
                Minimum = int.MinValue, Maximum = int.MaxValue, DecimalPlaces = 0,
                Value = saved[i], Width = 100, Anchor = AnchorStyles.Left,
                AccessibleName = "Модификатор: " + name, TextAlign = HorizontalAlignment.Right
            };
            modifiers[i] = number;
            table.Controls.Add(number, 1, i + 1);
            var button = new Button {
                Text = "Бросить", Width = 100, Height = 36, Anchor = AnchorStyles.Left,
                AccessibleName = "Бросить проверку: " + name, UseVisualStyleBackColor = true
            };
            buttons[i] = button;
            button.Click += (_, _) => Roll(index);
            table.Controls.Add(button, 2, i + 1);
            var faces = new TableLayoutPanel { ColumnCount = 4, RowCount = 1, Width = 208, Height = 46, Anchor = AnchorStyles.Left, Margin = Padding.Empty };
            for (var die = 0; die < 4; die++)
            {
                faces.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                var face = MakeLabel("?", 23, FontStyle.Bold);
                face.TextAlign = ContentAlignment.MiddleCenter;
                face.BorderStyle = BorderStyle.FixedSingle;
                face.Margin = new Padding(3);
                face.AccessibleName = $"{name}, кубик {die + 1}: ещё не брошен";
                dice[i, die] = face;
                faces.Controls.Add(face, die, 0);
            }
            table.Controls.Add(faces, 3, i + 1);
            var total = MakeLabel("—", 22, FontStyle.Bold);
            total.AccessibleName = "Результат: " + name;
            totals[i] = total;
            table.Controls.Add(total, 4, i + 1);
            // The displayed result is a snapshot of the modifier used in that roll.
            number.ValueChanged += (_, _) => ClearResult(index);
        }
        details.Dock = DockStyle.Fill;
        details.TextAlign = ContentAlignment.MiddleLeft;
        details.Font = new Font("Segoe UI", 10);
        details.Text = "Каждый кубик: 2 грани «+», 2 грани «−», 2 пустые.\n«+» = +1 · «−» = −1 · пустая грань = 0. Итог = модификатор + сумма кубиков.";
        details.AccessibleName = "Расшифровка результата";
        outer.Controls.Add(details, 0, 3);
        FormClosing += (_, _) => {
            if (statePath is not null && !ModifierStore.Save(statePath, modifiers.Select(n => (int)n.Value).ToArray()))
                MessageBox.Show(this, "Не удалось сохранить модификаторы в профиль пользователя.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
    }

    private static Label MakeLabel(string text, float size, FontStyle style = FontStyle.Regular) => new() {
        Text = text, Dock = DockStyle.Fill, Font = new Font("Segoe UI", size, style),
        TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
    };

    private void Roll(int index)
    {
        // Commit text typed into the numeric input before reading its value.
        modifiers[index].Validate();
        var check = FateRoller.Roll((int)modifiers[index].Value, nextFace);
        results[index] = check;
        for (var die = 0; die < 4; die++)
        {
            var value = check.Dice[die];
            dice[index, die].Text = FateRoller.Symbol(value);
            dice[index, die].AccessibleName = $"{FateRoller.Approaches[index]}, кубик {die + 1}: "
                + (value == 0 ? "пусто, ноль" : value == 1 ? "плюс один" : "минус один");
        }
        totals[index].Text = FateRoller.Signed(check.Total);
        details.Text = $"{FateRoller.Approaches[index]}: модификатор {FateRoller.Signed(check.Modifier)} + кубики ({FateRoller.Signed(check.DiceSum)}) = {FateRoller.Signed(check.Total)}\n"
            + "«+» = +1 · «−» = −1 · пустая грань = 0";
    }

    private void ClearResult(int index)
    {
        results[index] = null;
        totals[index].Text = "—";
        for (var die = 0; die < 4; die++)
        {
            dice[index, die].Text = "?";
            dice[index, die].AccessibleName = $"{FateRoller.Approaches[index]}, кубик {die + 1}: ещё не брошен";
        }
        details.Text = $"Модификатор «{FateRoller.Approaches[index]}» изменён. Нажмите «Бросить» для новой проверки.";
    }

    internal void SetModifier(int index, int value) => modifiers[index].Value = value;
    internal void TypeModifier(int index, string text) => modifiers[index].Text = text;
    internal void ClickRoll(int index) => buttons[index].PerformClick();
    internal string FaceText(int index, int die) => dice[index, die].Text;
    internal string TotalText(int index) => totals[index].Text;
    internal CheckResult? Result(int index) => results[index];
}
