using static GameState.TileType;

// Map layouts: 14 rows x 14 cols.
// Characters: # Stone  . Soil  S RiverSource  R River  B Bank  V Village  = Gate
//             G GoldSource  C ClaySource
// Col 0 of any non-zero region is overridden at runtime by river exit rows — put . there.
public static class MapLayouts
{
    public static readonly string[][][] Maps =
    [
        // Zone 0: Lowlands
        [
            // Region 0 — the river runs east from the source but is broken by a 2-tile gap
            // (row 6, cols 6-7). The upstream half (cols 1-5) is watered from the start so a
            // gold autopanner can be built beside it immediately; the downstream half (cols
            // 8-13) is laid but dry until the player digs the gap to reconnect it, which
            // carries flow to the east edge and opens the next map.
            [
                "##############",
                "#............#",
                "#............#",
                "#............#",
                "#............#",
                "#............#",
                "SRRRRR..RRRRRR",
                "#............#",
                "#............#",
                "#............#",
                "#............#",
                "#............#",
                "#............#",
                "##############",
            ],
            // Region 1 — village/gate map; col 0 set dynamically from region 0 exits
            [
                "#######V#####=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                ".............=",
                "#############=",
            ],
            // Region 2 — second village (Marl's Lowmarsh). Reached past the region-1
            // gate; col 0 set dynamically from region 1 exits. East edge is Stone
            // (terminal — no further gate yet). The village entrance sits on the north
            // edge (row 0, col 7), flanked by Soil at (0,6)/(0,8) and open below so a
            // river can feed it from several sides (a single neighbour couldn't reach
            // Marl's 150 threshold). Stone clusters make a longer, stonier channel.
            [
                "######.V.#####",
                ".............#",
                "..####.......#",
                "..#..........#",
                ".......####..#",
                ".............#",
                ".............#",
                ".............#",
                "..####.......#",
                "..#..........#",
                ".............#",
                ".......###...#",
                ".............#",
                "##############",
            ],
        ],
        // Zone 1: Highlands
        [
            // Region 0
            [
                "##############",
                "##############",
                "##RRRRR#######",
                "##RG##R#######",
                "##R###R#######",
                "##R###R#######",
                "SRR...RR###RRR",
                "####C##R###R##",
                "#######R###R##",
                "#######RRRRR##",
                "##############",
                "##############",
                "##############",
                "##############",
            ],
        ],
    ];

    public static GameState.TileType[,] BuildTiles(string[] map)
    {
        var tiles = new GameState.TileType[GameState.Rows, GameState.Cols];
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                tiles[row, col] = CharToTile(map[row][col]);
        return tiles;
    }

    public static float[,] BuildGold()
        => new float[GameState.Rows, GameState.Cols];

    public static float[,] BuildClay()
        => new float[GameState.Rows, GameState.Cols];

    public static GameState.TileType CharToTile(char c) => c switch
    {
        '#' => Stone,
        'S' => RiverSource,
        'R' => River,
        'B' => Soil, // Bank tiles removed from gameplay; legacy 'B' maps to Soil
        'V' => Village,
        '=' => Gate,
        'G' => GoldSource,
        'C' => ClaySource,
        _ => Soil,
    };
}
