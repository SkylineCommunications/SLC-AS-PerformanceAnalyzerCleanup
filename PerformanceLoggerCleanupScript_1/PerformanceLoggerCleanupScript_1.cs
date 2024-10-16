/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
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

16. 10. 2024	1.0.0.1		RCO, Skyline	Initial version
****************************************************************************
*/

namespace PerformanceLoggerCleanupScript_1
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Skyline.DataMiner.Automation;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        private readonly string folderPath = @"C:\Skyline_Data\PerformanceLogger";
        private DateTime oldestPerformanceInfoDateTime;
        private List<string> fileNamesToDelete;
        private List<string> fileNamesToEdit;

        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(IEngine engine)
        {
            try
            {
                RunSafe(engine);
            }
            catch (Exception e)
            {
                engine.ExitFail("Run|Something went wrong: " + e);
            }
        }

        private void RunSafe(IEngine engine)
        {
            Initialize(engine);

            if (!Directory.Exists(folderPath))
                return;

            DetermineFilesToDeleteOrEdit();
            engine.Log($"Files to delete {String.Join(", ", fileNamesToDelete)}");
            engine.Log($"Files to modify {String.Join(", ", fileNamesToEdit)}");
            HandleFilesToEditOrDelete();
        }

        private void HandleFilesToEditOrDelete()
        {
            foreach (string fileName in fileNamesToDelete)
            {
                File.Delete(fileName);
            }
        }

        private void DetermineFilesToDeleteOrEdit()
        {
            string[] files = Directory.GetFiles(folderPath);

            foreach (string file in files)
            {
                if (File.GetLastWriteTime(file) < oldestPerformanceInfoDateTime)
                {
                    fileNamesToDelete.Add(file);
                }
                else if (File.GetCreationTime(file) < oldestPerformanceInfoDateTime)
                {
                    fileNamesToEdit.Add(file);
                }
                else
                {
                    // do nothing
                }
            }
        }

        private void Initialize(IEngine engine)
        {
            var inputOfDays = Convert.ToInt32(engine.GetScriptParam("Days of oldest performance info").Value);
            oldestPerformanceInfoDateTime = DateTime.Now.AddDays(-inputOfDays);
            fileNamesToDelete = new List<string>();
            fileNamesToEdit = new List<string>();
        }
    }
}
