namespace Template;

public class Form : libs.Form
{
    public static void ExampleEvent(Dictionary<string, dynamic> bindings)
    {
        int max = int.Parse(bindings["clickmeEntry"]);
        string text = "";
        foreach (int y in Enumerable.Range(0, max))
        {
            if (y == 0 | y == max - 1)
            {
                text += String.Concat(Enumerable.Repeat("X", max));
            }
            else
            {
                text += "X" + String.Concat(Enumerable.Repeat(" ", max)) + "X";
            }

            text += "\n";
        }
    
        bindings["data_out"].Update(text);
    }
}