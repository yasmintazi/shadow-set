#!/bin/sh
# type-checks the game scripts against Unity's libraries without opening Unity
U="/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data"; M="$U/Managed/UnityEngine"
cd "$(dirname "$0")/.."
"$U/MonoBleedingEdge/bin/mono.exe" "$U/MonoBleedingEdge/lib/mono/4.5/mcs.exe" -langversion:7 -nologo -nowarn:618 -target:library -out:Tools/obj/check.dll "-r:$U/MonoBleedingEdge/lib/mono/4.5/Facades/netstandard.dll" "-r:/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/Managed/UnityEngine/Unity.Scripting.dll" "-r:$M/UnityEngine.CoreModule.dll" "-r:$M/UnityEngine.IMGUIModule.dll" "-r:$M/UnityEngine.AudioModule.dll" "-r:$M/UnityEngine.InputLegacyModule.dll" "-r:$M/UnityEngine.ScreenCaptureModule.dll" "-r:$M/UnityEngine.TextRenderingModule.dll" "-r:$M/UnityEngine.PhysicsModule.dll" Assets/Scripts/Sim/*.cs Assets/Scripts/Sim/Bots/*.cs Assets/Scripts/Game/*.cs && echo "scripts ok"
