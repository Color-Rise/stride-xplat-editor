// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a9968d1f-7e75-4d89-8411-27390a47e4d0")]

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies

[assembly: InternalsVisibleTo("Stride.Assets" + Stride.PublicKeys.Default)]
[assembly: InternalsVisibleTo("Stride.Assets.Presentation.Wpf" + Stride.PublicKeys.Default)]
