// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : ZZZRawImage.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages raw image, that is, user data sector by sector copy.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiscImageChef.CommonTypes;
using DiscImageChef.Console;
using DiscImageChef.Filters;

namespace DiscImageChef.DiscImages
{
    public class ZZZRawImage : IWritableImage
    {
        bool       differentTrackZeroSize;
        string     extension;
        ImageInfo  imageInfo;
        IFilter    rawImageFilter;
        FileStream writingStream;

        public ZZZRawImage()
        {
            imageInfo = new ImageInfo
            {
                ReadableSectorTags    = new List<SectorTagType>(),
                ReadableMediaTags     = new List<MediaTagType>(),
                HasPartitions         = false,
                HasSessions           = false,
                Version               = null,
                Application           = null,
                ApplicationVersion    = null,
                Creator               = null,
                Comments              = null,
                MediaManufacturer     = null,
                MediaModel            = null,
                MediaSerialNumber     = null,
                MediaBarcode          = null,
                MediaPartNumber       = null,
                MediaSequence         = 0,
                LastMediaSequence     = 0,
                DriveManufacturer     = null,
                DriveModel            = null,
                DriveSerialNumber     = null,
                DriveFirmwareRevision = null
            };
        }

        public string Name => "Raw Disk Image";
        // Non-random UUID to recognize this specific plugin
        public Guid      Id   => new Guid("12345678-AAAA-BBBB-CCCC-123456789000");
        public ImageInfo Info => imageInfo;

        public string Format => "Raw disk image (sector by sector copy)";

        public List<Track> Tracks
        {
            get
            {
                if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                    throw new FeatureUnsupportedImageException("Feature not supported by image format");

                Track trk = new Track
                {
                    TrackBytesPerSector    = (int)imageInfo.SectorSize,
                    TrackEndSector         = imageInfo.Sectors - 1,
                    TrackFile              = rawImageFilter.GetFilename(),
                    TrackFileOffset        = 0,
                    TrackFileType          = "BINARY",
                    TrackRawBytesPerSector = (int)imageInfo.SectorSize,
                    TrackSequence          = 1,
                    TrackStartSector       = 0,
                    TrackSubchannelType    = TrackSubchannelType.None,
                    TrackType              = TrackType.Data,
                    TrackSession           = 1
                };
                List<Track> lst = new List<Track> {trk};
                return lst;
            }
        }

        public List<Session> Sessions
        {
            get
            {
                if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                    throw new FeatureUnsupportedImageException("Feature not supported by image format");

                Session sess = new Session
                {
                    EndSector       = imageInfo.Sectors - 1,
                    EndTrack        = 1,
                    SessionSequence = 1,
                    StartSector     = 0,
                    StartTrack      = 1
                };
                List<Session> lst = new List<Session> {sess};
                return lst;
            }
        }

        public List<Partition> Partitions
        {
            get
            {
                if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                    throw new FeatureUnsupportedImageException("Feature not supported by image format");

                List<Partition> parts = new List<Partition>();
                Partition       part  = new Partition
                {
                    Start    = 0,
                    Length   = imageInfo.Sectors,
                    Offset   = 0,
                    Sequence = 0,
                    Type     = "MODE1/2048",
                    Size     = imageInfo.Sectors * imageInfo.SectorSize
                };
                parts.Add(part);
                return parts;
            }
        }

        public bool Identify(IFilter imageFilter)
        {
            // Check if file is not multiple of 512
            if(imageFilter.GetDataForkLength() % 512 == 0) return true;

            extension = Path.GetExtension(imageFilter.GetFilename())?.ToLower();

            if(extension == ".hdf" && imageInfo.ImageSize % 256 == 0) return true;

            // Check known disk sizes with sectors smaller than 512
            switch(imageFilter.GetDataForkLength())
            {
                #region Commodore
                case 174848:
                case 175531:
                case 197376:
                case 351062:
                case 822400:
                #endregion Commodore

                case 81664:
                case 116480:
                case 242944:
                case 256256:
                case 287488:
                case 306432:
                case 495872:
                case 988416:
                case 995072:
                case 1021696:
                case 1146624:
                case 1177344:
                case 1222400:
                case 1304320:
                case 1255168: return true;
                default:      return false;
            }
        }

        public bool Open(IFilter imageFilter)
        {
            Stream stream = imageFilter.GetDataForkStream();
            stream.Seek(0, SeekOrigin.Begin);

            extension = Path.GetExtension(imageFilter.GetFilename())?.ToLower();
            switch(extension)
            {
                case ".iso" when imageFilter.GetDataForkLength() % 2048 == 0:
                    imageInfo.SectorSize = 2048;
                    break;
                case ".d81" when imageFilter.GetDataForkLength() == 819200:
                    imageInfo.SectorSize = 256;
                    break;
                default:
                    if((extension                           == ".adf" || extension                       == ".adl" ||
                        extension                           == ".ssd" || extension                       == ".dsd") &&
                       (imageFilter.GetDataForkLength()     == 163840 || imageFilter.GetDataForkLength() == 327680 ||
                        imageFilter.GetDataForkLength()     == 655360)) imageInfo.SectorSize = 256;
                    else if((extension                      == ".adf" || extension == ".adl") &&
                            imageFilter.GetDataForkLength() == 819200)
                        imageInfo.SectorSize = 1024;
                    else
                        switch(imageFilter.GetDataForkLength())
                        {
                            case 242944:
                            case 256256:
                            case 495872:
                            case 92160:
                            case 133120:
                                imageInfo.SectorSize = 128;
                                break;
                            case 116480:
                            case 287488:  // T0S0 = 128bps
                            case 988416:  // T0S0 = 128bps
                            case 995072:  // T0S0 = 128bps, T0S1 = 256bps
                            case 1021696: // T0S0 = 128bps, T0S1 = 256bps
                            case 232960:
                            case 143360:
                            case 286720:
                            case 512512:
                            case 102400:
                            case 204800:
                            case 655360:
                            case 80384:  // T0S0 = 128bps
                            case 325632: // T0S0 = 128bps, T0S1 = 256bps
                            case 653312: // T0S0 = 128bps, T0S1 = 256bps

                            #region Commodore
                            case 174848:
                            case 175531:
                            case 196608:
                            case 197376:
                            case 349696:
                            case 351062:
                            case 822400:
                                #endregion Commodore

                                imageInfo.SectorSize = 256;
                                break;
                            case 81664:
                                imageInfo.SectorSize = 319;
                                break;
                            case 306432:  // T0S0 = 128bps
                            case 1146624: // T0S0 = 128bps, T0S1 = 256bps
                            case 1177344: // T0S0 = 128bps, T0S1 = 256bps
                                imageInfo.SectorSize = 512;
                                break;
                            case 1222400: // T0S0 = 128bps, T0S1 = 256bps
                            case 1304320: // T0S0 = 128bps, T0S1 = 256bps
                            case 1255168: // T0S0 = 128bps, T0S1 = 256bps
                            case 1261568:
                            case 1638400:
                                imageInfo.SectorSize = 1024;
                                break;
                            default:
                                imageInfo.SectorSize = 512;
                                break;
                        }

                    break;
            }

            imageInfo.ImageSize            = (ulong)imageFilter.GetDataForkLength();
            imageInfo.CreationTime         = imageFilter.GetCreationTime();
            imageInfo.LastModificationTime = imageFilter.GetLastWriteTime();
            imageInfo.MediaTitle           = Path.GetFileNameWithoutExtension(imageFilter.GetFilename());
            differentTrackZeroSize         = false;
            rawImageFilter                 = imageFilter;

            switch(imageFilter.GetDataForkLength())
            {
                case 242944:
                    imageInfo.Sectors = 1898;
                    break;
                case 256256:
                    imageInfo.Sectors = 2002;
                    break;
                case 495872:
                    imageInfo.Sectors = 3874;
                    break;
                case 116480:
                    imageInfo.Sectors = 455;
                    break;
                case 287488: // T0S0 = 128bps
                    imageInfo.Sectors      = 1136;
                    differentTrackZeroSize = true;
                    break;
                case 988416: // T0S0 = 128bps
                    imageInfo.Sectors      = 3874;
                    differentTrackZeroSize = true;
                    break;
                case 995072: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 3900;
                    differentTrackZeroSize = true;
                    break;
                case 1021696: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 4004;
                    differentTrackZeroSize = true;
                    break;
                case 81664:
                    imageInfo.Sectors = 256;
                    break;
                case 306432: // T0S0 = 128bps
                    imageInfo.Sectors      = 618;
                    differentTrackZeroSize = true;
                    break;
                case 1146624: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 2272;
                    differentTrackZeroSize = true;
                    break;
                case 1177344: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 2332;
                    differentTrackZeroSize = true;
                    break;
                case 1222400: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 1236;
                    differentTrackZeroSize = true;
                    break;
                case 1304320: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 1316;
                    differentTrackZeroSize = true;
                    break;
                case 1255168: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 1268;
                    differentTrackZeroSize = true;
                    break;
                case 80384: // T0S0 = 128bps
                    imageInfo.Sectors      = 322;
                    differentTrackZeroSize = true;
                    break;
                case 325632: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 1280;
                    differentTrackZeroSize = true;
                    break;
                case 653312: // T0S0 = 128bps, T0S1 = 256bps
                    imageInfo.Sectors      = 2560;
                    differentTrackZeroSize = true;
                    break;
                case 1880064: // IBM XDF, 3,5", real number of sectors
                    imageInfo.Sectors      = 670;
                    imageInfo.SectorSize   = 8192; // Biggest sector size
                    differentTrackZeroSize = true;
                    break;
                case 175531:
                    imageInfo.Sectors = 683;
                    break;
                case 197375:
                    imageInfo.Sectors = 768;
                    break;
                case 351062:
                    imageInfo.Sectors = 1366;
                    break;
                case 822400:
                    imageInfo.Sectors = 3200;
                    break;
                default:
                    imageInfo.Sectors = imageInfo.ImageSize / imageInfo.SectorSize;
                    break;
            }

            imageInfo.MediaType = CalculateDiskType();

            switch(imageInfo.MediaType)
            {
                case MediaType.CD:
                case MediaType.DVDPR:
                case MediaType.DVDR:
                case MediaType.DVDRDL:
                case MediaType.DVDPRDL:
                case MediaType.BDR:
                case MediaType.BDRXL:
                    imageInfo.XmlMediaType = XmlMediaType.OpticalDisc;
                    break;
                default:
                    imageInfo.XmlMediaType = XmlMediaType.BlockMedia;
                    break;
            }

            // Sharp X68000 SASI hard disks
            if(extension                     == ".hdf")
                if(imageInfo.ImageSize % 256 == 0)
                {
                    imageInfo.SectorSize = 256;
                    imageInfo.Sectors    = imageInfo.ImageSize / imageInfo.SectorSize;
                    imageInfo.MediaType  = MediaType.GENERIC_HDD;
                }

            if(imageInfo.XmlMediaType == XmlMediaType.OpticalDisc)
            {
                imageInfo.HasSessions   = true;
                imageInfo.HasPartitions = true;
            }

            DicConsole.VerboseWriteLine("Raw disk image contains a disk of type {0}", imageInfo.MediaType);

            switch(imageInfo.MediaType)
            {
                case MediaType.ACORN_35_DS_DD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 5;
                    break;
                case MediaType.ACORN_35_DS_HD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.ACORN_525_DS_DD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 16;
                    break;
                case MediaType.ACORN_525_SS_DD_40:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 16;
                    break;
                case MediaType.ACORN_525_SS_DD_80:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 16;
                    break;
                case MediaType.ACORN_525_SS_SD_40:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.ACORN_525_SS_SD_80:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.Apple32DS:
                    imageInfo.Cylinders       = 35;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 13;
                    break;
                case MediaType.Apple32SS:
                    imageInfo.Cylinders       = 36;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 13;
                    break;
                case MediaType.Apple33DS:
                    imageInfo.Cylinders       = 35;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 16;
                    break;
                case MediaType.Apple33SS:
                    imageInfo.Cylinders       = 35;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 16;
                    break;
                case MediaType.AppleSonyDS:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.AppleSonySS:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.ATARI_35_DS_DD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.ATARI_35_DS_DD_11:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 11;
                    break;
                case MediaType.ATARI_35_SS_DD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.ATARI_35_SS_DD_11:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 11;
                    break;
                case MediaType.ATARI_525_ED:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 26;
                    break;
                case MediaType.ATARI_525_SD:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 18;
                    break;
                case MediaType.CBM_35_DD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.CBM_AMIGA_35_DD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 11;
                    break;
                case MediaType.CBM_AMIGA_35_HD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 22;
                    break;
                case MediaType.DMF:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 21;
                    break;
                case MediaType.DOS_35_DS_DD_9:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 9;
                    break;
                case MediaType.Apricot_35:
                    imageInfo.Cylinders       = 70;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 9;
                    break;
                case MediaType.DOS_35_ED:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 36;
                    break;
                case MediaType.DOS_35_HD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 18;
                    break;
                case MediaType.DOS_35_SS_DD_9:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 9;
                    break;
                case MediaType.DOS_525_DS_DD_8:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.DOS_525_DS_DD_9:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 9;
                    break;
                case MediaType.DOS_525_HD:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 15;
                    break;
                case MediaType.DOS_525_SS_DD_8:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.DOS_525_SS_DD_9:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 9;
                    break;
                case MediaType.ECMA_54:
                    imageInfo.Cylinders       = 77;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 26;
                    break;
                case MediaType.ECMA_59:
                    imageInfo.Cylinders       = 77;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 26;
                    break;
                case MediaType.ECMA_66:
                    imageInfo.Cylinders       = 35;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 9;
                    break;
                case MediaType.ECMA_69_8:
                    imageInfo.Cylinders       = 77;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.ECMA_70:
                    imageInfo.Cylinders       = 40;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 16;
                    break;
                case MediaType.ECMA_78:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 16;
                    break;
                case MediaType.ECMA_99_15:
                    imageInfo.Cylinders       = 77;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 15;
                    break;
                case MediaType.ECMA_99_26:
                    imageInfo.Cylinders       = 77;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 26;
                    break;
                case MediaType.ECMA_99_8:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.FDFORMAT_35_DD:
                    imageInfo.Cylinders       = 82;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 10;
                    break;
                case MediaType.FDFORMAT_35_HD:
                    imageInfo.Cylinders       = 82;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 21;
                    break;
                case MediaType.FDFORMAT_525_HD:
                    imageInfo.Cylinders       = 82;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 17;
                    break;
                case MediaType.IBM23FD:
                    imageInfo.Cylinders       = 32;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.IBM33FD_128:
                    imageInfo.Cylinders       = 73;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 26;
                    break;
                case MediaType.IBM33FD_256:
                    imageInfo.Cylinders       = 74;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 15;
                    break;
                case MediaType.IBM33FD_512:
                    imageInfo.Cylinders       = 74;
                    imageInfo.Heads           = 1;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.IBM43FD_128:
                    imageInfo.Cylinders       = 74;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 26;
                    break;
                case MediaType.IBM43FD_256:
                    imageInfo.Cylinders       = 74;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 15;
                    break;
                case MediaType.IBM53FD_1024:
                    imageInfo.Cylinders       = 74;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.IBM53FD_256:
                    imageInfo.Cylinders       = 74;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 26;
                    break;
                case MediaType.IBM53FD_512:
                    imageInfo.Cylinders       = 74;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 15;
                    break;
                case MediaType.NEC_35_TD:
                    imageInfo.Cylinders       = 240;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 38;
                    break;
                case MediaType.NEC_525_HD:
                    imageInfo.Cylinders       = 77;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 8;
                    break;
                case MediaType.XDF_35:
                    imageInfo.Cylinders       = 80;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 23;
                    break;
                // Following ones are what the device itself report, not the physical geometry
                case MediaType.Jaz:
                    imageInfo.Cylinders       = 1021;
                    imageInfo.Heads           = 64;
                    imageInfo.SectorsPerTrack = 32;
                    break;
                case MediaType.PocketZip:
                    imageInfo.Cylinders       = 154;
                    imageInfo.Heads           = 16;
                    imageInfo.SectorsPerTrack = 32;
                    break;
                case MediaType.LS120:
                    imageInfo.Cylinders       = 963;
                    imageInfo.Heads           = 8;
                    imageInfo.SectorsPerTrack = 32;
                    break;
                case MediaType.LS240:
                    imageInfo.Cylinders       = 262;
                    imageInfo.Heads           = 32;
                    imageInfo.SectorsPerTrack = 56;
                    break;
                case MediaType.FD32MB:
                    imageInfo.Cylinders       = 1024;
                    imageInfo.Heads           = 2;
                    imageInfo.SectorsPerTrack = 32;
                    break;
                default:
                    imageInfo.Cylinders       = (uint)(imageInfo.Sectors / 16 / 63);
                    imageInfo.Heads           = 16;
                    imageInfo.SectorsPerTrack = 63;
                    break;
            }

            return true;
        }

        public byte[] ReadSector(ulong sectorAddress)
        {
            return ReadSectors(sectorAddress, 1);
        }

        public byte[] ReadSectors(ulong sectorAddress, uint length)
        {
            if(differentTrackZeroSize) throw new NotImplementedException("Not yet implemented");

            if(sectorAddress > imageInfo.Sectors - 1)
                throw new ArgumentOutOfRangeException(nameof(sectorAddress), "Sector address not found");

            if(sectorAddress + length > imageInfo.Sectors)
                throw new ArgumentOutOfRangeException(nameof(length), "Requested more sectors than available");

            byte[] buffer = new byte[length * imageInfo.SectorSize];

            Stream stream = rawImageFilter.GetDataForkStream();

            stream.Seek((long)(sectorAddress * imageInfo.SectorSize), SeekOrigin.Begin);

            stream.Read(buffer, 0, (int)(length * imageInfo.SectorSize));

            return buffer;
        }

        public bool? VerifySector(ulong sectorAddress)
        {
            return null;
        }

        public bool? VerifySector(ulong sectorAddress, uint track)
        {
            return null;
        }

        public bool? VerifySectors(ulong sectorAddress, uint length, out List<ulong> failingLbas,
                                   out                                   List<ulong> unknownLbas)
        {
            failingLbas = new List<ulong>();
            unknownLbas = new List<ulong>();

            for(ulong i = sectorAddress; i < sectorAddress + length; i++) unknownLbas.Add(i);

            return null;
        }

        public bool? VerifySectors(ulong sectorAddress, uint length, uint track, out List<ulong> failingLbas,
                                   out                                               List<ulong> unknownLbas)
        {
            failingLbas = new List<ulong>();
            unknownLbas = new List<ulong>();

            for(ulong i = sectorAddress; i < sectorAddress + length; i++) unknownLbas.Add(i);

            return null;
        }

        public bool? VerifyMediaImage()
        {
            return null;
        }

        public List<Track> GetSessionTracks(Session session)
        {
            if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                throw new FeatureUnsupportedImageException("Feature not supported by image format");

            if(session.SessionSequence != 1)
                throw new ArgumentOutOfRangeException(nameof(session), "Only a single session is supported");

            Track trk = new Track
            {
                TrackBytesPerSector    = (int)imageInfo.SectorSize,
                TrackEndSector         = imageInfo.Sectors - 1,
                TrackFilter            = rawImageFilter,
                TrackFile              = rawImageFilter.GetFilename(),
                TrackFileOffset        = 0,
                TrackFileType          = "BINARY",
                TrackRawBytesPerSector = (int)imageInfo.SectorSize,
                TrackSequence          = 1,
                TrackStartSector       = 0,
                TrackSubchannelType    = TrackSubchannelType.None,
                TrackType              = TrackType.Data,
                TrackSession           = 1
            };
            List<Track> lst = new List<Track> {trk};
            return lst;
        }

        public List<Track> GetSessionTracks(ushort session)
        {
            if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                throw new FeatureUnsupportedImageException("Feature not supported by image format");

            if(session != 1)
                throw new ArgumentOutOfRangeException(nameof(session), "Only a single session is supported");

            Track trk = new Track
            {
                TrackBytesPerSector    = (int)imageInfo.SectorSize,
                TrackEndSector         = imageInfo.Sectors - 1,
                TrackFilter            = rawImageFilter,
                TrackFile              = rawImageFilter.GetFilename(),
                TrackFileOffset        = 0,
                TrackFileType          = "BINARY",
                TrackRawBytesPerSector = (int)imageInfo.SectorSize,
                TrackSequence          = 1,
                TrackStartSector       = 0,
                TrackSubchannelType    = TrackSubchannelType.None,
                TrackType              = TrackType.Data,
                TrackSession           = 1
            };
            List<Track> lst = new List<Track> {trk};
            return lst;
        }

        public byte[] ReadSector(ulong sectorAddress, uint track)
        {
            if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                throw new FeatureUnsupportedImageException("Feature not supported by image format");

            if(track != 1) throw new ArgumentOutOfRangeException(nameof(track), "Only a single track is supported");

            return ReadSector(sectorAddress);
        }

        public byte[] ReadSectors(ulong sectorAddress, uint length, uint track)
        {
            if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                throw new FeatureUnsupportedImageException("Feature not supported by image format");

            if(track != 1) throw new ArgumentOutOfRangeException(nameof(track), "Only a single track is supported");

            return ReadSectors(sectorAddress, length);
        }

        public byte[] ReadSectorLong(ulong sectorAddress, uint track)
        {
            if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                throw new FeatureUnsupportedImageException("Feature not supported by image format");

            if(track != 1) throw new ArgumentOutOfRangeException(nameof(track), "Only a single track is supported");

            return ReadSector(sectorAddress);
        }

        public byte[] ReadSectorsLong(ulong sectorAddress, uint length, uint track)
        {
            if(imageInfo.XmlMediaType != XmlMediaType.OpticalDisc)
                throw new FeatureUnsupportedImageException("Feature not supported by image format");

            if(track != 1) throw new ArgumentOutOfRangeException(nameof(track), "Only a single track is supported");

            return ReadSectors(sectorAddress, length);
        }

        public byte[] ReadSectorTag(ulong sectorAddress, SectorTagType tag)
        {
            throw new FeatureUnsupportedImageException("Feature not supported by image format");
        }

        public byte[] ReadSectorsTag(ulong sectorAddress, uint length, SectorTagType tag)
        {
            throw new FeatureUnsupportedImageException("Feature not supported by image format");
        }

        public byte[] ReadSectorLong(ulong sectorAddress)
        {
            throw new FeatureUnsupportedImageException("Feature not supported by image format");
        }

        public byte[] ReadSectorsLong(ulong sectorAddress, uint length)
        {
            throw new FeatureUnsupportedImageException("Feature not supported by image format");
        }

        public byte[] ReadDiskTag(MediaTagType tag)
        {
            throw new FeatureUnsupportedImageException("Feature not supported by image format");
        }

        public byte[] ReadSectorTag(ulong sectorAddress, uint track, SectorTagType tag)
        {
            throw new FeatureUnsupportedImageException("Feature not supported by image format");
        }

        public byte[] ReadSectorsTag(ulong sectorAddress, uint length, uint track, SectorTagType tag)
        {
            throw new FeatureUnsupportedImageException("Feature not supported by image format");
        }

        // TODO: Support media tags as separate files
        public IEnumerable<MediaTagType> SupportedMediaTags => new MediaTagType[] { };

        public IEnumerable<SectorTagType> SupportedSectorTags => new SectorTagType[] { };
        public IEnumerable<MediaType>     SupportedMediaTypes
        {
            get
            {
                List<MediaType> types = new List<MediaType>();
                foreach(MediaType type in Enum.GetValues(typeof(MediaType)))
                    switch(type)
                    {
                        // TODO: Implement support for writing formats with different track 0 bytes per sector
                        case MediaType.IBM33FD_256:
                        case MediaType.IBM33FD_512:
                        case MediaType.IBM43FD_128:
                        case MediaType.IBM43FD_256:
                        case MediaType.IBM53FD_256:
                        case MediaType.IBM53FD_512:
                        case MediaType.IBM53FD_1024:
                        case MediaType.ECMA_99_8:
                        case MediaType.ECMA_99_15:
                        case MediaType.ECMA_99_26:
                        case MediaType.ECMA_66:
                        case MediaType.ECMA_69_8:
                        case MediaType.ECMA_69_15:
                        case MediaType.ECMA_69_26:
                        case MediaType.ECMA_70:
                        case MediaType.ECMA_78: continue;
                        default:
                            types.Add(type);
                            break;
                    }

                return types;
            }
        }

        public IEnumerable<(string name, Type type, string description)> SupportedOptions =>
            new (string name, Type type, string description)[] { };
        public IEnumerable<string> KnownExtensions =>
            new[] {".adf", ".adl", ".d81", ".dsk", ".hdf", ".ima", ".img", ".iso", ".ssd", ".st"};
        public bool   IsWriting    { get; private set; }
        public string ErrorMessage { get; private set; }

        public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                           uint   sectorSize)
        {
            if(sectorSize == 0)
            {
                ErrorMessage = "Unsupported sector size";
                return false;
            }

            if(!SupportedMediaTypes.Contains(mediaType))
            {
                ErrorMessage = $"Unsupport media format {mediaType}";
                return false;
            }

            imageInfo = new ImageInfo {MediaType = mediaType, SectorSize = sectorSize, Sectors = sectors};

            try { writingStream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None); }
            catch(IOException e)
            {
                ErrorMessage = $"Could not create new image file, exception {e.Message}";
                return false;
            }

            IsWriting    = true;
            ErrorMessage = null;
            return true;
        }

        public bool SetGeometry(uint cylinders, uint heads, uint sectorsPerTrack)
        {
            // Geometry is not stored in image
            return true;
        }

        public bool WriteSectorTag(byte[] data, ulong sectorAddress, SectorTagType tag)
        {
            ErrorMessage = "Unsupported feature";
            return false;
        }

        public bool WriteSectorsTag(byte[] data, ulong sectorAddress, uint length, SectorTagType tag)
        {
            ErrorMessage = "Unsupported feature";
            return false;
        }

        public bool WriteMediaTag(byte[] data, MediaTagType tag)
        {
            // TODO: Implement
            ErrorMessage = "Writing media tags is not supported.";
            return false;
        }

        public bool WriteSector(byte[] data, ulong sectorAddress)
        {
            if(!IsWriting)
            {
                ErrorMessage = "Tried to write on a non-writable image";
                return false;
            }

            if(data.Length != imageInfo.SectorSize)
            {
                ErrorMessage = "Incorrect data size";
                return false;
            }

            if(sectorAddress >= imageInfo.Sectors)
            {
                ErrorMessage = "Tried to write past image size";
                return false;
            }

            writingStream.Seek((long)(sectorAddress * imageInfo.SectorSize), SeekOrigin.Begin);
            writingStream.Write(data, 0, data.Length);

            ErrorMessage = "";
            return true;
        }

        public bool WriteSectors(byte[] data, ulong sectorAddress, uint length)
        {
            if(!IsWriting)
            {
                ErrorMessage = "Tried to write on a non-writable image";
                return false;
            }

            if(data.Length % imageInfo.SectorSize != 0)
            {
                ErrorMessage = "Incorrect data size";
                return false;
            }

            if(sectorAddress + length > imageInfo.Sectors)
            {
                ErrorMessage = "Tried to write past image size";
                return false;
            }

            writingStream.Seek((long)(sectorAddress * imageInfo.SectorSize), SeekOrigin.Begin);
            writingStream.Write(data, 0, data.Length);

            ErrorMessage = "";
            return true;
        }

        public bool WriteSectorLong(byte[] data, ulong sectorAddress)
        {
            ErrorMessage = "Writing sectors with tags is not supported.";
            return false;
        }

        public bool WriteSectorsLong(byte[] data, ulong sectorAddress, uint length)
        {
            ErrorMessage = "Writing sectors with tags is not supported.";
            return false;
        }

        public bool SetTracks(List<Track> tracks)
        {
            if(tracks.Count <= 1) return true;

            ErrorMessage = "This format supports only 1 track";
            return false;
        }

        public bool Close()
        {
            if(!IsWriting)
            {
                ErrorMessage = "Image is not opened for writing";
                return false;
            }

            writingStream.Flush();
            writingStream.Close();
            IsWriting = false;

            return true;
        }

        public bool SetMetadata(ImageInfo metadata)
        {
            return true;
        }

        MediaType CalculateDiskType()
        {
            if(imageInfo.SectorSize == 2048)
            {
                if(imageInfo.Sectors <= 360000) return MediaType.CD;
                if(imageInfo.Sectors <= 2295104) return MediaType.DVDPR;
                if(imageInfo.Sectors <= 2298496) return MediaType.DVDR;
                if(imageInfo.Sectors <= 4171712) return MediaType.DVDRDL;
                if(imageInfo.Sectors <= 4173824) return MediaType.DVDPRDL;
                if(imageInfo.Sectors <= 24438784) return MediaType.BDR;

                return imageInfo.Sectors <= 62500864 ? MediaType.BDRXL : MediaType.Unknown;
            }

            switch(imageInfo.ImageSize)
            {
                case 80384:  return MediaType.ECMA_66;
                case 81664:  return MediaType.IBM23FD;
                case 92160:  return MediaType.ATARI_525_SD;
                case 102400: return MediaType.ACORN_525_SS_SD_40;
                case 116480: return MediaType.Apple32SS;
                case 133120: return MediaType.ATARI_525_ED;
                case 143360: return MediaType.Apple33SS;
                case 163840:
                    if(imageInfo.SectorSize == 256) return MediaType.ACORN_525_SS_DD_40;

                    return MediaType.DOS_525_SS_DD_8;
                case 184320: return MediaType.DOS_525_SS_DD_9;
                case 204800: return MediaType.ACORN_525_SS_SD_80;
                case 232960: return MediaType.Apple32DS;
                case 242944: return MediaType.IBM33FD_128;
                case 256256: return MediaType.ECMA_54;
                case 286720: return MediaType.Apple33DS;
                case 287488: return MediaType.IBM33FD_256;
                case 306432: return MediaType.IBM33FD_512;
                case 322560: return MediaType.Apricot_35;
                case 325632: return MediaType.ECMA_70;
                case 327680:
                    if(imageInfo.SectorSize == 256) return MediaType.ACORN_525_SS_DD_80;

                    return MediaType.DOS_525_DS_DD_8;
                case 368640:
                    if(extension == ".st") return MediaType.DOS_35_SS_DD_9;

                    return MediaType.DOS_525_DS_DD_9;
                case 409600:
                    if(extension == ".st") return MediaType.ATARI_35_SS_DD;

                    return MediaType.AppleSonySS;
                case 450560: return MediaType.ATARI_35_SS_DD_11;
                case 495872: return MediaType.IBM43FD_128;
                case 512512: return MediaType.ECMA_59;
                case 653312: return MediaType.ECMA_78;
                case 655360: return MediaType.ACORN_525_DS_DD;
                case 737280: return MediaType.DOS_35_DS_DD_9;
                case 819200:
                    if(imageInfo.SectorSize == 256) return MediaType.CBM_35_DD;
                    if((extension           == ".adf" || extension == ".adl") && imageInfo.SectorSize == 1024)
                        return MediaType.ACORN_35_DS_DD;
                    if(extension == ".st") return MediaType.ATARI_35_DS_DD;

                    return MediaType.AppleSonyDS;
                case 839680: return MediaType.FDFORMAT_35_DD;
                case 901120:
                    if(extension == ".st") return MediaType.ATARI_35_DS_DD_11;

                    return MediaType.CBM_AMIGA_35_DD;
                case 988416:     return MediaType.IBM43FD_256;
                case 995072:     return MediaType.IBM53FD_256;
                case 1021696:    return MediaType.ECMA_99_26;
                case 1146624:    return MediaType.IBM53FD_512;
                case 1177344:    return MediaType.ECMA_99_15;
                case 1222400:    return MediaType.IBM53FD_1024;
                case 1228800:    return MediaType.DOS_525_HD;
                case 1255168:    return MediaType.ECMA_69_8;
                case 1261568:    return MediaType.NEC_525_HD;
                case 1304320:    return MediaType.ECMA_99_8;
                case 1427456:    return MediaType.FDFORMAT_525_HD;
                case 1474560:    return MediaType.DOS_35_HD;
                case 1638400:    return MediaType.ACORN_35_DS_HD;
                case 1720320:    return MediaType.DMF;
                case 1763328:    return MediaType.FDFORMAT_35_HD;
                case 1802240:    return MediaType.CBM_AMIGA_35_HD;
                case 1880064:    return MediaType.XDF_35;
                case 1884160:    return MediaType.XDF_35;
                case 2949120:    return MediaType.DOS_35_ED;
                case 9338880:    return MediaType.NEC_35_TD;
                case 33554432:   return MediaType.FD32MB;
                case 40387584:   return MediaType.PocketZip;
                case 126222336:  return MediaType.LS120;
                case 127923200:  return MediaType.ECMA_154;
                case 201410560:  return MediaType.HiFD;
                case 229632000:  return MediaType.ECMA_201;
                case 240386048:  return MediaType.LS240;
                case 481520640:  return MediaType.ECMA_183_512;
                case 533403648:  return MediaType.ECMA_183;
                case 596787200:  return MediaType.ECMA_184_512;
                case 654540800:  return MediaType.ECMA_184;
                case 1070617600: return MediaType.Jaz;

                #region Commodore
                case 174848:
                case 175531: return MediaType.CBM_1540;
                case 196608:
                case 197376: return MediaType.CBM_1540_Ext;
                case 349696:
                case 351062: return MediaType.CBM_1571;
                #endregion Commodore

                default: return MediaType.GENERIC_HDD;
            }
        }
    }
}