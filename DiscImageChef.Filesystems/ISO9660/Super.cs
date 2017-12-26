﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ISO9660 filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles mounting and umounting the ISO9660 filesystem.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2018 Natalia Portillo
// ****************************************************************************/

using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.DiscImages;

namespace DiscImageChef.Filesystems.ISO9660
{
    public partial class ISO9660
    {
        public virtual Errno Mount(IMediaImage imagePlugin, Partition partition, Encoding encoding, bool debug)
        {
            return Errno.NotImplemented;
        }

        public virtual Errno Unmount()
        {
            return Errno.NotImplemented;
        }

        public virtual Errno StatFs(ref FileSystemInfo stat)
        {
            return Errno.NotImplemented;
        }
    }
}