# MoreHeadUtilities

Adds additional functionality to MoreHead, allowing cosmetics to hide body parts.

## How to use

1. Install another plugin which has used the PartShrinker script
2. Select the cosmetic through MoreHead as normal
3. The set part(s) should be hidden

## Example

![MoreHeadMenu](https://raw.githubusercontent.com/Maygik/MoreHeadUtilities/refs/heads/master/Shared/MoreHeadMenu.png)

## For Developers

To setup this mod on your cosmetic
1. Install and import the [unity plugin](https://github.com/Maygik/MoreHeadUtilities/raw/refs/heads/master/MoreHeadUtilities.unitypackage) into your Unity project
2. Follow the standard [MoreHead](https://thunderstore.io/c/repo/p/YMC_MHZ/MoreHead/) development to just before using the Head Decorations Builder
3. Add the PartShrinker component to the empty (although object in the accessory technically works). "World" accessories cannot hide parts.

    ![UnityComponent](https://raw.githubusercontent.com/Maygik/MoreHeadUtilities/refs/heads/master/Shared/UnityComponent.png)

4. Setup which part should be hidden in the component properties, and whether the part's child parts should also be hidden

    ![BodyParts](https://raw.githubusercontent.com/Maygik/MoreHeadUtilities/refs/heads/master/Shared/BodyParts.png)

5. Continue with MoreHead setup as normal

### Hierarchy for Child Parts
```
Hips
    > Left Leg
    > Right Leg
    > Body
        > Left Arm
        > Right Arm
        > Neck
            > Health
            > Head
                > Left Eye
                    > Left Pupil
                > Right Eye
                    > Right Pupil
```



## Updates
- 1.0.3
	- Massively improved performance when opening MoreHead menu
	- Added config to enable/disable logging
- 1.0.2
	- Separated Eye and Pupil hiding
	- Backwards compatible (Although if "Hide Children" was disabled on eyes from an older version, it will still keep the pupils)

- 1.0.1
    - Fixed README images

- 1.0.0
    - Initial Upload