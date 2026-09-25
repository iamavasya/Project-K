export function base64ToBlob(base64: string): Blob {
  const byteString = atob(base64.split(',')[1]);
  const arrayBuffer = new ArrayBuffer(byteString.length);
  const int8Array = new Uint8Array(arrayBuffer);

  for (let i = 0; i < byteString.length; i++) {
    int8Array[i] = byteString.codePointAt(i)!;
  }
  const mimeType = /^data:([^;,]+)/.exec(base64)?.[1] ?? 'image/png';
  return new Blob([int8Array], { type: mimeType });
}
