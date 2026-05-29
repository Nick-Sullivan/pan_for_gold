# TODO

Automated tests that run godot and interact with the game. Ideally, there should be an easy way for it to determine the current game state (an API endpoint, perhaps?) and confirm the state is as it should be.
For example, starting a new game and confirming that we're now in the first map.

This is key - automated tests for regression + setting up the game to a particular state is essential for quick development of this game. Getting one feature completed and thoroughly tested is more important than a wide set of features.

The game loads to an opening screen, with new game / load game. It should have 3 possible save slots to use.

There should be a main quest guiding the user. First, it is to pan for gold to buy a shovel. Then, it is to use the shovel to build a river to the edge of the map, requiring the user to remove existing river tiles as they explore how the flow system work. Then, it is to supply enough flow to a village on the second map. Upon discovering there isn't enough, it opens the ability to go to the highlands, where they can adjust what resources flow into the lowlands, to be panned. They swap from gold to clay, collect the clay, and create brick so that water from the river doesn't lose flow, allowing enough flow to provide water to the river. 
The game continues this way - the user must build rivers to meet certain criteria, adjusting the highlands accordingly to get the resources needed to build for the lowlands. With these resources, they gain access to new structures and tools, and they can improve their earlier maps in the lowlands to improve performance. This includes upgrades like stronger shovels, plants to grow food, pumps to jump water over undiggable terrain, auto resource collection.
The art style should be satisfying, clean, and cute. The joy comes from solving the self contained puzzles, and by being able to see their maps becoming more and more efficient at generating resources and solving the puzzles - maps that previously required critical thinking to solve within resource constraints become trivial with more resources and technologies.
When a village is discovered, a character from that village talks to the player saying what they need. The village art, character demeanor, and clothing should all be unique and memorable for that village.
The map should be a grid, with the river flowing back and forth down to the last village.



