using System.Runtime.CompilerServices;
using System.Windows;

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                //  application, or any theme specific resource dictionary)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
)]

// The test project exercises internal seams (result mapping, the test-only
// engine constructor) without loading the models.
[assembly: InternalsVisibleTo("CapParse.Tests")]
