/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace IDP_Info_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AdaptiveCards;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		private enum SoftwareStatus
		{
			NotAvailable = -1,
			Unknown = 0,
			UpToDate = 1,
			Running = 2,
			Outdated = 3,
		}

		private readonly Dictionary<SoftwareStatus, string> SoftwareStatusIcons = new Dictionary<SoftwareStatus, string>
		{
			{ SoftwareStatus.Unknown, "\u2753"}, // Question Mark
			{ SoftwareStatus.Outdated, "\u274C" }, // Red Cross Mark
		};

		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			try
			{
				var request = engine.GetScriptParam("Request").Value;

				switch (request)
				{
					case "Managed":
						GetManagedDevices(engine);
						break;

					case "Unmanaged":
						GetUnManagedDevices(engine);
						break;

					case "Not Software Compliant":
						GetNotSoftwareCompliantElements(engine);
						break;

					default:
						throw new NotSupportedException($"Request '{request}' is not supported.");
				}
			}
			catch (Exception ex)
			{
				engine.GenerateInformation(ex.Message);
				engine.AddScriptOutput("ERROR", ex.Message);
			}
		}

		private static string ConvertToString(string value, string exceptionValue)
		{
			if (String.IsNullOrEmpty(value))
			{
				return "N/A";
			}

			return value != exceptionValue
				? value
				: "N/A";
		}

		private string ConvertSoftwareStatusToString(SoftwareStatus status)
		{
			if (status == SoftwareStatus.NotAvailable)
			{
				return "N/A";
			}

			if (SoftwareStatusIcons.ContainsKey(status))
			{
				return SoftwareStatusIcons[status];
			}

			return status.ToString();
		}

		private void GetManagedDevices(IEngine engine)
		{
			var dms = engine.GetDms();
			var idpElement = dms.GetElement("DataMiner IDP");

			var gptm = new GetPartialTableMessage
			{
				DataMinerID = idpElement.DmsElementId.AgentId,
				ElementID = idpElement.DmsElementId.ElementId,
				ParameterID = 1100,
				Filters = new[] { String.Empty },
			};

			var pcem = (ParameterChangeEventMessage)Engine.SLNet.SendSingleResponseMessage(gptm);

			var tableRows = new List<AdaptiveTableRow>
			{
				new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("Name")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("IP Address")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("CI Type")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
					},
				},
			};

			for (int rowIdx = 0; rowIdx < pcem.NewValue.ArrayValue[0].ArrayValue.Length; rowIdx++)
			{
				// Detect if script outputs doesn't grow too large
				if (rowIdx >= 100)
				{
					break;
				}

				tableRows.Add(new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							// Name
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[3].ArrayValue[rowIdx].CellValue.StringValue,
										null))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// IP Address
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[9].ArrayValue[rowIdx].CellValue.StringValue,
										String.Empty))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// CI Type
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[10].ArrayValue[rowIdx].CellValue.StringValue,
										String.Empty))
								{
									Type = "TextBlock",
								},
							},
						},
					},
				});
			}

			List<AdaptiveElement> scriptOutput = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock("Managed Devices")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Bolder,
					Size = AdaptiveTextSize.Large,
				},
			};
			if (tableRows.Count == 1)
			{
				scriptOutput.Add(new AdaptiveTextBlock("There are no managed devices.")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Default,
					Size = AdaptiveTextSize.Default,
				});
			}
			else
			{
				scriptOutput.Add(new AdaptiveTable
				{
					Type = "Table",
					FirstRowAsHeaders = true,
					Columns = new List<AdaptiveTableColumnDefinition>
					{
						new AdaptiveTableColumnDefinition
						{
							Width = 200,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 100,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 250,
						},
					},
					Rows = tableRows,
				});
			}

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(scriptOutput));
		}

		private void GetUnManagedDevices(IEngine engine)
		{
			var dms = engine.GetDms();
			var idpElement = dms.GetElement("DataMiner IDP");

			var gptm = new GetPartialTableMessage
			{
				DataMinerID = idpElement.DmsElementId.AgentId,
				ElementID = idpElement.DmsElementId.ElementId,
				ParameterID = 1900,
				Filters = new[] { String.Empty },
			};

			var pcem = (ParameterChangeEventMessage)Engine.SLNet.SendSingleResponseMessage(gptm);

			var tableRows = new List<AdaptiveTableRow>
			{
				new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("Name")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("IP Address")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("CI Type")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
					},
				},
			};

			for (int rowIdx = 0; rowIdx < pcem.NewValue.ArrayValue[0].ArrayValue.Length; rowIdx++)
			{
				// Detect if script outputs doesn't grow too large
				if (rowIdx >= 100)
				{
					break;
				}

				// Detected IP Address
				if (String.IsNullOrEmpty(pcem.NewValue.ArrayValue[5].ArrayValue[rowIdx].CellValue.StringValue))
				{
					continue;
				}

				tableRows.Add(new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							// Name
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[2].ArrayValue[rowIdx].CellValue.StringValue,
										null))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// Detected IP Address
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[5].ArrayValue[rowIdx].CellValue.StringValue,
										String.Empty))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// Detected CI Type
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[3].ArrayValue[rowIdx].CellValue.StringValue,
										String.Empty))
								{
									Type = "TextBlock",
								},
							},
						},
					},
				});
			}

			List<AdaptiveElement> scriptOutput = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock("Unmanaged Devices")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Bolder,
					Size = AdaptiveTextSize.Large,
				},
			};
			if (tableRows.Count == 1)
			{
				scriptOutput.Add(new AdaptiveTextBlock("There are no unmanaged devices.")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Default,
					Size = AdaptiveTextSize.Default,
				});
			}
			else
			{
				scriptOutput.Add(new AdaptiveTable
				{
					Type = "Table",
					FirstRowAsHeaders = true,
					Columns = new List<AdaptiveTableColumnDefinition>
					{
						new AdaptiveTableColumnDefinition
						{
							Width = 200,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 100,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 250,
						},
					},
					Rows = tableRows,
				});
			}

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(scriptOutput));
		}

		private void GetNotSoftwareCompliantElements(IEngine engine)
		{
			var dms = engine.GetDms();
			var idpElement = dms.GetElement("DataMiner IDP");

			var gptm = new GetPartialTableMessage
			{
				DataMinerID = idpElement.DmsElementId.AgentId,
				ElementID = idpElement.DmsElementId.ElementId,
				ParameterID = 1700,
				Filters = new[] { String.Empty },
			};

			var pcem = (ParameterChangeEventMessage)Engine.SLNet.SendSingleResponseMessage(gptm);

			var tableRows = new List<AdaptiveTableRow>
			{
				new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("Status")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("CI Type")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("Element Name")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("IP Address")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock("Detected Software Version")
								{
									Type = "TextBlock",
									Weight = AdaptiveTextWeight.Bolder,
								},
							},
						},
					},
				},
			};

			SoftwareStatus status;
			for (int rowIdx = 0; rowIdx < pcem.NewValue.ArrayValue[0].ArrayValue.Length; rowIdx++)
			{
				// Check if the output doesn't become too large.
				if (rowIdx >= 100)
				{
					break;
				}

				status = (SoftwareStatus)pcem.NewValue.ArrayValue[1].ArrayValue[rowIdx].CellValue.DoubleValue;
				if (status == SoftwareStatus.Running || status == SoftwareStatus.UpToDate)
				{
					// Only the not compliant.
					continue;
				}

				tableRows.Add(new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							// Status
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(ConvertSoftwareStatusToString(status))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// CI Type
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[2].ArrayValue[rowIdx].CellValue.StringValue,
										null))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// Element Name
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[3].ArrayValue[rowIdx].CellValue.StringValue,
										null))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// IP Address
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[4].ArrayValue[rowIdx].CellValue.StringValue,
										null))
								{
									Type = "TextBlock",
								},
							},
						},
						new AdaptiveTableCell
						{
							// Detected Software Version
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(
									ConvertToString(
										pcem.NewValue.ArrayValue[7].ArrayValue[rowIdx].CellValue.StringValue,
										null))
								{
									Type = "TextBlock",
								},
							},
						},
					},
				});
			}

			List<AdaptiveElement> scriptOutput = new List<AdaptiveElement>
			{
				new AdaptiveTextBlock("Not Software Compliant Elements")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Bolder,
					Size = AdaptiveTextSize.Large,
				},
			};
			if (tableRows.Count == 1)
			{
				scriptOutput.Add(new AdaptiveTextBlock("The software of each element is up to date.")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Default,
					Size = AdaptiveTextSize.Default,
				});
			}
			else
			{
				scriptOutput.Add(new AdaptiveTable
				{
					Type = "Table",
					FirstRowAsHeaders = true,
					Columns = new List<AdaptiveTableColumnDefinition>
					{
						new AdaptiveTableColumnDefinition
						{
							Width = 150,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 150,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 150,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 150,
						},
						new AdaptiveTableColumnDefinition
						{
							Width = 150,
						},
					},
					Rows = tableRows,
				});
			}

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(scriptOutput));
		}
	}
}