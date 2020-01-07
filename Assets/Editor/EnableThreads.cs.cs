using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
class EnableThreads
{
    static EnableThreads()
    {
        Debug.Log("Enable WebGL Threads");
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.memorySize = 1024;
        //PlayerSettings.WebGL.memorySize = 768;
        //PlayerSettings.WebGL.memorySize = 512;
        //PlayerSettings.WebGL.emscriptenArgs = "";
        //PlayerSettings.WebGL.emscriptenArgs = "-s \"BINARYEN_TRAP_MODE='clamp'\"";
        //PlayerSettings.WebGL.memorySize = 512;
    }
}