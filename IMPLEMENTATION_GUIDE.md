# Seed Inventory Hotbar & Pot Planting Implementation Guide

This guide will walk you through implementing the clickable seed inventory slots and pot planting system.

## Overview

The system allows players to:
1. Click a seed slot in the hotbar to select it (slot highlights in yellow)
2. Click an empty pot to plant the selected seed
3. The seed count decreases by 1
4. The pot sprite changes to show a sprout

## Files Created/Modified

### New Files:
- `Assets/Scripts/PotManager.cs` - Manages individual pot interactions

### Modified Files:
- `Assets/Scripts/GardenHotbar.cs` - Added click handlers and selection system

## Step-by-Step Implementation

### Step 1: Prepare Your Sprite Assets

1. Make sure you have your **sprout pot sprite** ready in your project
2. Note the location/name of the sprite (you'll need it in Step 3)

### Step 2: Setup GardenHotbar (Already Done)

The `GardenHotbar` script has been updated with:
- Click handlers for seed slots
- Selection system (selected slots highlight in yellow)
- Singleton pattern for easy access

**No action needed** - the code is already in place!

### Step 3: Add PotManager to Each Pot

For each pot GameObject in your scene:

1. **Select the pot GameObject** in the Hierarchy (e.g., "Plant Pot", "Plant Pot (1)", etc.)

2. **Add the PotManager Component:**
   - In the Inspector, click "Add Component"
   - Search for "Pot Manager" and add it

3. **Configure the PotManager:**
   - **Empty Pot Sprite**: Drag your current empty pot sprite here (the one currently showing)
   - **Sprout Pot Sprite**: Drag your sprout pot sprite here
   - **Pot Image**: 
     - If the pot has an Image component, drag it here
     - If not, leave it empty - the script will auto-find it
   - **Show Debug Logs**: Enable this if you want to see debug messages

4. **Repeat for all pots** in your scene

### Step 4: Verify Button Components

The `PotManager` script automatically:
- Adds a Button component if one doesn't exist
- Sets up click handlers

**No action needed** - this happens automatically!

### Step 5: Test the System

1. **Enter Play Mode**
2. **Buy a seed** from the shop (e.g., ginger root)
3. **Click a seed slot** in the hotbar - it should highlight in yellow
4. **Click an empty pot** - the seed should be planted:
   - Seed count decreases by 1
   - Pot sprite changes to sprout
   - Selection clears

## Troubleshooting

### Seeds don't appear in hotbar
- Make sure you're buying seeds (not herbs)
- Check that the seed itemID contains "seed" (e.g., "ginger_seed")
- Enable "Show Debug Logs" in GardenHotbar to see what's happening

### Pots aren't clickable
- Make sure the PotManager component is added to each pot
- Check that the pot GameObject has a RectTransform (UI element)
- Verify the pot is on a Canvas

### Pot sprite doesn't change
- Make sure you've assigned the "Sprout Pot Sprite" in PotManager
- Check that the "Pot Image" field is assigned (or the pot has an Image component)
- Enable "Show Debug Logs" in PotManager to see error messages

### Selection doesn't work
- Make sure GardenHotbar has all slot GameObjects assigned in the Inspector
- Check that each slot GameObject has an Image component for the icon
- Enable "Show Debug Logs" in GardenHotbar

## Customization

### Change Selection Color

In `GardenHotbar` Inspector:
- Find "Selection Visual" section
- Adjust "Selected Color" (default is yellow tint)

### Change Sprout Sprite Per Pot

Each pot can have a different sprout sprite:
- Select the pot
- In PotManager component, assign a different "Sprout Pot Sprite"

## Code Structure

### GardenHotbar
- `OnSlotClicked(int slotIndex)` - Handles seed slot clicks
- `SelectSlot(int slotIndex)` - Highlights selected slot
- `ClearSelection()` - Clears selection (called after planting)
- `GetSelectedSeedID()` - Returns currently selected seed ID

### PotManager
- `OnPotClicked()` - Handles pot clicks
- `PlantSeed(string seedID)` - Plants seed and updates pot sprite
- `IsPlanted()` - Check if pot has a seed
- `ResetPot()` - Reset pot to empty (for testing)

## Next Steps (Optional Enhancements)

1. **Save/Load Pot States** - Save which pots are planted across game sessions
2. **Plant Growth System** - Add stages between sprout and harvest
3. **Visual Feedback** - Add particle effects or animations when planting
4. **Sound Effects** - Add audio feedback for planting actions

