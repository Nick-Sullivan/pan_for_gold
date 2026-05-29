public record TileCell(GameState.TileType Type)
{
    public static implicit operator TileCell(GameState.TileType type) => new(type);
}
