# SLC-AS-ChatOps-IDP

This repository contains an automation script solution with scripts that can be used to retrieve IDP information from your DataMiner system using the DataMiner Teams bot.

The following scrips are currently available:

- [Get Managed Devices](#Get-Managed-Devices)

- [Get Unmanaged Devices](#Get-Unmanaged-Devices)

- [Get Not-Software Compliant Elements](#Get-Not-Software-Compliant-Elements)

## Pre-requisites

Kindly ensure that your DataMiner system and your Microsoft Teams adhere to the pre-requisites described in [DM Docs](https://docs.dataminer.services/user-guide/Cloud_Platform/TeamsBot/Microsoft_Teams_Chat_Integration.html#server-side-prerequisites).

## Get Managed Devices

Automation script returns the managed elements of the IDP solution in a table. A message given in case no element of the DataMiner system is managed by the IDP solution.

## Get-Managed-Devices

Automation script returns the unmanaged elements by the IDP solution in a table. A message given in case all the elements of the DataMiner system are managed by the IDP solution.

## Get Not-Software Compliant Elements

Automation script returns returns the elements that are identified by IDP as Not-Software Compliant. A message given in case the software of all the elements is up-to-date.

**Limitations:**

    Due to the limited size of a bot message (40 KB), are the tables limited to a 100 rows.