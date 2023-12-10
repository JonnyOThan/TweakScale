Scaling doesn't seem to work correctly with B9PS.

The FL-t400 tank costs 500 at normal scale and full of fuel.  The normal dry cost should be 316.4.

When scaled up to 2.5m (linear scale of 2) and TS/L and b9ps are installed, it costs 4000 dry and ??? wet.
With TS/R, it costs 2531 dry and 4000 wet - exactly what it should - at least on the default subtype
but with the other subtypes, changing the scale does weird things.

B9PS has its own scale tracking system, but simply zeroing out the modifiers if the part has a b9ps module doesn't work either.

B9PS takes scale into account in its GetModuleCost function.  but does it not apply for the default subtype maybe?
Do we need to force scale to 1 before getting the module costs?

```
B9_TANK_TYPE
{
	name = LFOX
	tankMass = 0.000625
	tankCost = 0.00
}

B9_TANK_TYPE
{
	name = LF
	tankMass = 0.000625
	tankCost = 0.3
}
```

fl-t400 config:

```
	baseVolume = 400
	cost = 500
	mass = 0.25

	SUBTYPE
	{
		name = LF/O
		title = LF/Ox
		tankType = LFOX
		addedMass = -0.25
		addedCost = -183.6
	}

	SUBTYPE
	{
		name = LiquidFuel
		title = LF
		tankType = LF
		addedMass = -0.25
		addedCost = -183.6
	}
```


How does b9ps actually calculate its cost and mass?

b9ps module cost is:
```
GetTotalVolume(subtype) * subtype.tankType.TotalUnitCost + subtype.addedCost * VolumeScale;
GeTotalVolume is:
(baseVolume * subtype.volumeMultiplier + subtype.volumeAdded + VolumeFromChildren) * VolumeScale
```
baseVolume comes from the config

TotalUnitCost => ResourceUnitCost + tankCost;

How does the stock game calculate cost?

// basically, prefab cost plus module costs minus maximum resource cost plus current resource cost

```
float num2 = partInfo.cost + part.GetModuleCosts(partInfo.cost);
float num3 = 0f;
int count2 = part.Resources.Count;
while (count2-- > 0)
{
	PartResource partResource = part.Resources[count2];
	PartResourceDefinition info = partResource.info;
	num2 -= info.unitCost * (float)partResource.maxAmount;
	num3 += info.unitCost * (float)partResource.amount;
}
dryCost += num2;
fuelCost += num3;
```