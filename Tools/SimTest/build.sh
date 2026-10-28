#!/bin/sh
# compiles the simulation with the level-design tool, using the Mono that comes with Unity
U="/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/MonoBleedingEdge"
cd "$(dirname "$0")"
"$U/bin/mono.exe" "$U/lib/mono/4.5/mcs.exe" -langversion:7 -nologo -out:SimTest.exe ../../Assets/Scripts/Sim/*.cs ../../Assets/Scripts/Sim/Bots/*.cs *.cs && echo built
