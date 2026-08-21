using ProjectOdyssey;

public class GameplayScreen : IGameScreen
{
    private GameSession session;
    private GameplayRenderer gameplayRenderer;
    private ChartData chartData;

    public GameplayScreen(ChartData chartData, InputHistory inputHistory)
    {
        this.chartData = chartData;
        session = new GameSession(inputHistory);
        gameplayRenderer = new GameplayRenderer();
    }

    public void Initalise()
    {
        gameplayRenderer.Intitialise();
        gameplayRenderer.LoadTextures();

        for (int i = 0; i < chartData.notesByColumn.Length; i++)
            Console.WriteLine($"Column {i}: {chartData.notesByColumn[i].Length} notes");

        session.Start(chartData);
    }

    public void Render()
    {
        gameplayRenderer.DrawGameplay(session.notesByColumn, session.columnCursors);
    }

    public void Dispose()
    {
        session.Stop();
        gameplayRenderer.Dispose();
    }
}