# SFS-DeltaV-calculator

**What does this mod?**

This is a mod for SFS to add the rocket ΔV to your flight data information:
![DVcalculator](https://github.com/Kaskouy/SFS-DeltaV-calculator/assets/18355468/fb558b57-18ee-476b-8d95-3fe07f227c58)

The information is automatically calculated in real time. It takes into account all engines currently turned on and their available fuel sources, then it calculates the ΔV you would get by burning until you're out of fuel.

When you're running several stages at once (like a first stage flanked by boosters), it does anticipate the fact that the boosters will run out of fuel earlier than the first stage, but it doesn't take into account the booster separation. Instead it acts like if you kept the boosters attached until all the fuel is depleted. For this reason you can expect a sudden increase of your ΔV when you separate your empty boosters: it alleviates your rocket, so the new ΔV is higher.


**What is that "ΔV" thing?**

"ΔV" is to be pronounced "delta-V". "Δ" is a greek letter that is commonly used in physics to express a difference. In this case, it means "velocity difference". That's an abstract rocket resource that tells you by how much you can make vary your rocket speed.

For example, if your rocket runs at 1000 m/s and you accelerate until you reach 1100 m/s, then you have spent 100 m/s of ΔV.

**How to install it?**

- Go to [Releases](https://github.com/Kaskouy/SFS-DeltaV-calculator/releases), and download "DeltaV_Calculator.dll" (take the latest available version).
- Launch _Spaceflight simulator_, then from the _mod loader_ menu, click on _Open Mods Folder_
- Drop DeltaV_Calculator.dll into the opened folder, and restart your game. The mod is now available in the mod loader.
