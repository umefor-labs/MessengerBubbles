"""Renders promo/index.html to an MP4, or to stills with: render.py --stills OUTDIR 5 12.5 ..."""
import pathlib
import subprocess
import sys

from playwright.sync_api import sync_playwright

ROOT = pathlib.Path(__file__).resolve().parent
OUT = ROOT / "out"
FPS = 30
# Times the app's sound plays: it stays quiet for 3 seconds after each play, like the real app.
DINGS = [4.2, 7.6, 31.2]
WAV = ROOT.parent / "src" / "MessengerBubbles" / "Assets" / "new-message.wav"


def page_for(p):
    browser = p.chromium.launch(channel="chrome", headless=True)
    page = browser.new_page(viewport={"width": 1920, "height": 1080})
    page.goto((ROOT / "index.html").as_uri())
    page.evaluate("document.fonts.ready")
    page.wait_for_load_state("networkidle")
    return browser, page


def stills(out_dir, times):
    out = pathlib.Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    with sync_playwright() as p:
        browser, page = page_for(p)
        for t in times:
            page.evaluate(f"seek({t})")
            page.screenshot(path=str(out / f"t{float(t):05.1f}.jpg"), type="jpeg", quality=80)
        browser.close()


def video():
    OUT.mkdir(exist_ok=True)
    silent = OUT / "silent.mp4"
    final = OUT / "messenger-bubbles-intro.mp4"
    encoder = subprocess.Popen(
        ["ffmpeg", "-y", "-loglevel", "error", "-f", "image2pipe", "-framerate", str(FPS), "-c:v", "mjpeg", "-i", "-",
         "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(silent)],
        stdin=subprocess.PIPE)
    with sync_playwright() as p:
        browser, page = page_for(p)
        frames = int(page.evaluate("DURATION") * FPS)
        for i in range(frames):
            page.evaluate(f"seek({i / FPS})")
            encoder.stdin.write(page.screenshot(type="jpeg", quality=95))
            if i % 150 == 0:
                print(f"frame {i}/{frames}", flush=True)
        browser.close()
    encoder.stdin.close()
    encoder.wait()

    inputs, mix = ["-i", str(silent)], ""
    for n, t in enumerate(DINGS):
        inputs += ["-i", str(WAV)]
        ms = int(t * 1000)
        mix += f"[{n + 1}:a]adelay={ms}|{ms}[a{n}];"
    mix += "".join(f"[a{n}]" for n in range(len(DINGS))) + f"amix=inputs={len(DINGS)}:normalize=0,apad[a]"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", *inputs, "-filter_complex", mix, "-map", "0:v", "-map", "[a]",
                    "-c:v", "copy", "-c:a", "aac", "-b:a", "160k", "-shortest", "-movflags", "+faststart", str(final)], check=True)
    silent.unlink()
    print(final)


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--stills":
        stills(sys.argv[2], sys.argv[3:])
    else:
        video()
