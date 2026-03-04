export interface ApkRelease {
  id: string;
  appName: string;
  packageName: string;
  versionCode: number;
  versionName: string;
  uploadedAt: string;
  filePath: string;
  fileSizeBytes: number;
  originalFileName?: string | null;
}

