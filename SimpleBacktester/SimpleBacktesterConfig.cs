namespace SimpleBacktester
{
    public class SimpleBacktesterConfig
    {
        public string Strategy { get; set; }
        public object[] StrategyParams { get; set; }

        public BacktestConfig Base { get; set; }
        public BacktestConfig[] Backtests { get; set; }

        public bool RunWebVisualization { get; set; }
        public bool WebVisualizationDisplayMarks { get; set; }
    }
}
