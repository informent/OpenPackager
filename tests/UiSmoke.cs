using System.Diagnostics;
using System.Windows.Automation;

var exe = args.Length > 0 ? args[0] : throw new ArgumentException("Published executable path is required.");
using var process = Process.Start(exe) ?? throw new Exception("Could not start published GUI.");
AutomationElement? window = null;
for (var i = 0; i < 20 && window is null; i++)
{
    await Task.Delay(250);
    window = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id));
}
if (window is null) throw new Exception("Published window was not found.");
var combos = window.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox));
if (combos.Count < 2) throw new Exception($"Expected runtime and theme ComboBoxes, found {combos.Count}.");
foreach (AutomationElement combo in combos)
{
    if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
    {
        var dropdown = (ExpandCollapsePattern)pattern;
        dropdown.Expand(); await Task.Delay(100); dropdown.Collapse();
    }
}
var slider = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Slider));
if (slider is null) throw new Exception("Accent slider was not found.");
if (slider.TryGetCurrentPattern(RangeValuePattern.Pattern, out var rangePattern)) ((RangeValuePattern)rangePattern).SetValue(180);
if (process.HasExited) throw new Exception($"GUI exited during UI smoke test with code {process.ExitCode}.");
process.Kill(true);
Console.WriteLine("PASS: published UI opened runtime/theme dropdowns and changed accent slider");
