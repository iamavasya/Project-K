/**
 * A file the app made or fetched, handed to the person: the system share sheet where the phone
 * has one for files (iOS 15+, Android), else a plain download as the web does.
 */

export function canShareFile(file: File): boolean {
  try {
    return typeof navigator.share === 'function' && typeof navigator.canShare === 'function' && navigator.canShare({ files: [file] });
  } catch {
    return false;
  }
}

/**
 * Opens the share sheet. Call it straight from a tap: the browser refuses a share that no tap
 * started. Refused or failed, the file is saved instead; a share the person closed is left alone.
 */
export async function shareFile(file: File): Promise<void> {
  try {
    await navigator.share({ files: [file], title: file.name });
  } catch (error) {
    if ((error as DOMException | null)?.name !== 'AbortError') saveFile(file);
  }
}

/** The web's saveBlob: a download link clicked in code. */
export function saveFile(file: File): void {
  const url = URL.createObjectURL(file);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = file.name;
  anchor.rel = 'noopener';
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
  // Safari reads the link after the click returns; let it, then let the memory go.
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
