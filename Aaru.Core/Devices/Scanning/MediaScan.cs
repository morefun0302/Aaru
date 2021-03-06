// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MediaScan.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Scans media from devices.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using System;
using Aaru.CommonTypes.Enums;
using Aaru.Devices;

namespace Aaru.Core.Devices.Scanning
{
    public sealed partial class MediaScan
    {
        readonly Device _dev;
        readonly string _devicePath;
        readonly string _ibgLogPath;
        readonly string _mhddLogPath;
        readonly bool   _seekTest;
        bool            _aborted;

        /// <param name="mhddLogPath">Path to a MHDD log file</param>
        /// <param name="ibgLogPath">Path to a IMGBurn log file</param>
        /// <param name="devicePath">Device path</param>
        /// <param name="dev">Device</param>
        /// <param name="seekTest">Enable seek test</param>
        public MediaScan(string mhddLogPath, string ibgLogPath, string devicePath, Device dev, bool seekTest = true)
        {
            _mhddLogPath = mhddLogPath;
            _ibgLogPath  = ibgLogPath;
            _devicePath  = devicePath;
            _dev         = dev;
            _aborted     = false;
            _seekTest    = seekTest;
        }

        public ScanResults Scan()
        {
            switch(_dev.Type)
            {
                case DeviceType.ATA: return Ata();
                case DeviceType.MMC:
                case DeviceType.SecureDigital: return SecureDigital();
                case DeviceType.NVMe: return Nvme();
                case DeviceType.ATAPI:
                case DeviceType.SCSI: return Scsi();
                default: throw new NotSupportedException("Unknown device type.");
            }
        }

        public void Abort() => _aborted = true;

        /// <summary>Event raised when the progress bar is not longer needed</summary>
        public event EndProgressHandler EndProgress;
        /// <summary>Event raised when a progress bar is needed</summary>
        public event InitProgressHandler InitProgress;
        /// <summary>Event raised to report status updates</summary>
        public event UpdateStatusHandler UpdateStatus;
        /// <summary>Event raised to report a fatal error that stops the dumping operation and should call user's attention</summary>
        public event ErrorMessageHandler StoppingErrorMessage;
        /// <summary>Event raised to update the values of a determinate progress bar</summary>
        public event UpdateProgressHandler UpdateProgress;
        /// <summary>Event raised to update the status of an indeterminate progress bar</summary>
        public event PulseProgressHandler PulseProgress;
        public event ScanTimeHandler       ScanTime;
        public event ScanUnreadableHandler ScanUnreadable;
        public event InitBlockMapHandler   InitBlockMap;
        public event ScanSpeedHandler      ScanSpeed;
    }
}