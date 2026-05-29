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
            // Region 0
            [
                "##############",
                "#........B....",
                "#.RRRRRRRR....",
                "#.R....RB.....",
                "#.RBR..R......",
                "#.R.R.........",
                "SRR.RRRRRRRRRR",
                "#.R.R..RB.....",
                "#.R.R.BRB.....",
                "#.R.R.BBB.....",
                "#.R.R.........",
                "#.RRR.........",
                "#.............",
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
        'B' => Bank,
        'V' => Village,
        '=' => Gate,
        'G' => GoldSource,
        'C' => ClaySource,
        _ => Soil,
    };
}
