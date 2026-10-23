# P3D-Debinarizer-Arma-3
P3DDebinarizer converts binarized p3d models (ODOL format) to editable MLOD format

Latest and up to date P3D De-binarizer, activly maintained

## Usage

- Run `P3DDebinarizer.exe` without arguments to open the graphical interface.
- Add one or more `.p3d` files, or add a folder to queue all `.p3d` files in that folder (subfolders are not included).
- Optionally choose an output folder. If no output folder is selected, each converted file is saved beside its source with `_mlod` added to its name.
- Click **Debinarize** to process the queue. Conversion progress and per-file details are shown in the window.
- The existing command-line modes are also available: pass a `.p3d` path to convert one file, a folder path to convert its top-level files, or an input folder and output folder for batch conversion.

## Build

On Windows, install the .NET SDK and run the following from the repository root:

```powershell
dotnet build .\P3DDebinarizer.sln --configuration Release
```

The executable and its required `bisdll.dll` dependency are written to `bin\Release\net461\`. Distribute both files together. The app targets .NET Framework 4.6.1, so the .NET Framework 4.6.1 (or compatible later) runtime must be installed on the machine running it.
