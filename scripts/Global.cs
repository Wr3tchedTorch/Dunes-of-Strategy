public class Global
{
    public static bool InBattle = false;
    public static int TurnIndex = 0;
    public static int UnitsInBattle;
    public static void NextTurn()
    {
        TurnIndex++;
        if (TurnIndex > UnitsInBattle) TurnIndex = 0;
    }
    public static void UpdateTurn() {
        if (TurnIndex > UnitsInBattle) TurnIndex = 0;
    }
    public static void EndGame() => TurnIndex = -1;

    public static void StartGame() => TurnIndex = 0;
}