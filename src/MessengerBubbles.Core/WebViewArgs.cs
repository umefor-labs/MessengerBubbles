namespace MessengerBubbles.Core;

public static class WebViewArgs
{
    const string Base = "--disable-background-timer-throttling --disable-renderer-backgrounding --disk-cache-size=1";

    // Without the GPU process WebView2 draws in software: less memory, a little more CPU.
    public static string For(bool disableGpu) => disableGpu ? Base + " --disable-gpu" : Base;
}
