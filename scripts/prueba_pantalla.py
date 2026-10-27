import argparse
import io
import struct
import time

import libusb_package
import usb.core
import usb.util
from PIL import Image, ImageDraw, ImageFont

VID, PID = 0x87AD, 0x70DB
MAGIC = bytes([0x12, 0x34, 0x56, 0x78])
EP_OUT = 0x01
CHUNK = 16 * 1024


def handshake_packet() -> bytes:
    pkt = bytearray(64)
    pkt[0:4] = MAGIC
    pkt[56] = 1
    return bytes(pkt)


def frame_packet(jpeg: bytes, w: int, h: int) -> bytes:
    hdr = bytearray(64)
    hdr[0:4] = MAGIC
    struct.pack_into("<IIII", hdr, 4, 2, w, h, 0)
    struct.pack_into("<II", hdr, 56, 2, len(jpeg))
    return bytes(hdr) + jpeg


def dibujar(w: int, h: int, n: int) -> bytes:
    img = Image.new("RGB", (w, h), (14, 16, 22))
    d = ImageDraw.Draw(img)
    colores = [(230, 60, 60), (60, 200, 90), (60, 110, 240), (245, 245, 245)]
    for i, c in enumerate(colores):
        d.rectangle([i * w // 4, 0, (i + 1) * w // 4, h // 6], fill=c)
    try:
        grande = ImageFont.truetype("segoeuib.ttf", h // 6)
        chica = ImageFont.truetype("segoeui.ttf", h // 16)
    except OSError:
        grande = chica = ImageFont.load_default()
    d.text((w // 2, h // 2), "Vitral", font=grande, fill=(245, 240, 230), anchor="mm")
    d.text((w // 2, h // 2 + h // 7), f"cuadro {n}  ·  arriba-izquierda ↖", font=chica,
           fill=(170, 175, 190), anchor="mm")
    d.rectangle([0, h - 12, int(w * ((n % 50) / 49)), h], fill=(255, 176, 32))
    buf = io.BytesIO()
    img.save(buf, "JPEG", quality=90)
    return buf.getvalue()


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--enviar", type=float, default=0, help="segundos enviando cuadros")
    ap.add_argument("--w", type=int, default=0)
    ap.add_argument("--h", type=int, default=0)
    args = ap.parse_args()

    dev = usb.core.find(idVendor=VID, idProduct=PID, backend=libusb_package.get_libusb1_backend())
    if dev is None:
        raise SystemExit("No se encontró 87AD:70DB")
    try:
        dev.set_configuration()
    except usb.core.USBError as e:
        print("set_configuration:", e)
    cfg = dev.get_active_configuration()
    for intf in cfg:
        print(f"interfaz {intf.bInterfaceNumber} clase 0x{intf.bInterfaceClass:02x}")
        for ep in intf:
            print(f"  ep 0x{ep.bEndpointAddress:02x} tipo {ep.bmAttributes & 3} max {ep.wMaxPacketSize}")
    usb.util.claim_interface(dev, 0)
    try:
        dev.write(EP_OUT, handshake_packet(), 1000)
        resp = bytes(dev.read(0x81, 1024, 1000))
        pm, sub = resp[24], resp[36]
        print(f"respuesta {len(resp)} bytes  PM={pm} SUB={sub} resp[56]=0x{resp[56]:02x}")
        print("primeros 64:", resp[:64].hex(" "))
        if not args.enviar:
            return
        w = args.w or 640
        h = args.h or 480
        fin = time.monotonic() + args.enviar
        n = 0
        t0 = time.monotonic()
        while time.monotonic() < fin:
            pkt = frame_packet(dibujar(w, h, n), w, h)
            for i in range(0, len(pkt), CHUNK):
                dev.write(EP_OUT, pkt[i:i + CHUNK], 5000)
            if len(pkt) % 512 == 0:
                dev.write(EP_OUT, b"", 5000)
            n += 1
            time.sleep(0.1)
        dt = time.monotonic() - t0
        print(f"{n} cuadros en {dt:.1f} s ({n / dt:.1f} fps), último {len(pkt)} bytes")
    finally:
        usb.util.release_interface(dev, 0)
        usb.util.dispose_resources(dev)


if __name__ == "__main__":
    main()
