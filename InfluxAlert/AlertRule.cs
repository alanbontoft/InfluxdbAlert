public class AlertRule
{
    public string Name { get; set; }
    public string Type { get; set; }  // "threshold" or "deadman"
    public string Query { get; set; }
    public string Field { get; set; }
    public double Threshold { get; set; }
    public string Comparison { get; set; }
    public int Max_Age_Seconds { get; set; }
}
