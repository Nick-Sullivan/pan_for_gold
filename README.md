
This is an experimental game made with Claude and Godot.

## Installation

All dependencies are controlled with [mise](https://mise.jdx.dev/demo.html).

```sh
mise install
```

Then run it with 
```sh
mise run run
```

## Gameplay

The game is about a river of water carrying gold, and the players goal is to lead the river to their civilisation.

The game is part clicker game, and part factoria design game. 

There are two zones of the map, the Highlands, where we control the river flow to absorb metals. And the Lowlands, where we control the river to provide sufficient water and extract those metals. 

The map is a grid of tiles. To progress to the next tile, there may be certain requirements for the people who control that gate. For example, one civilisation may require a particular flow of water to allow to to proceed. If at any point a change in upstream rivers causes the flow rate to drop below the required amount, the gate will close again.

## Progression

Initially, the river has gold. Banks next to rivers slowly accumulate gold, which the player can pan to collect.

Once they have enough gold, they can purchase new tools, such as the shovel, which can be used to change the flow of the river.

To progress the river to their civilisation, they must destroy increasing hardness of stones. To do this they must build new equipment from harder resources, which must be added to the river flow of the Highlands.

No tutorial, all the mechanics should be simple enough to understand immediately.

## Mechanics

- Shaping the map tile: Use shovels to fill in river with soil, or dig up soil so the river can flow.
- River speed: Water flows into soil and banks, slowing it down. The speed of the river is effectively determined by its surface area.
- Gold/Silver/Copper/Iron etc.: Currency for purchasing or building new items.
- Crops: Produce resources needed for running equipment and keeping the ground sturdy.
- Soil: Different types of soil have different aptitudes for crops.
- Equipment: Some equipment is able to pan for gold, others automatically harvest crops, others convert one resource into another, others are like shovels but for very hard rocks/soil.

## Art style

Simple, clean, professional looking. Getting the art right and consistent is key for the enjoyment of the game. It must look satisying to see it operate.
