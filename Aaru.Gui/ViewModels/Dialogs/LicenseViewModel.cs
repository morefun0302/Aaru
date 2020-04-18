﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LicenseViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the license dialog.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2020 Natalia Portillo
// ****************************************************************************/

using System.IO;
using System.Reactive;
using System.Reflection;
using Aaru.Gui.Views.Dialogs;
using ReactiveUI;

namespace Aaru.Gui.ViewModels.Dialogs
{
    public class LicenseViewModel : ViewModelBase
    {
        readonly LicenseDialog _view;
        string                 _versionText;

        public LicenseViewModel(LicenseDialog view)
        {
            _view        = view;
            CloseCommand = ReactiveCommand.Create(ExecuteCloseCommand);

            using(Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Aaru.Gui.LICENSE"))
                using(var reader = new StreamReader(stream))
                {
                    LicenseText = reader.ReadToEnd();
                }
        }

        public string                      Title        => "Aaru's license";
        public string                      CloseLabel   => "Close";
        public string                      LicenseText  { get; }
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        void ExecuteCloseCommand() => _view.Close();
    }
}