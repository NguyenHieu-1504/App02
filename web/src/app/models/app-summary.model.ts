export interface AppSummary {
  packageName: string;
  appName: string;
  versionCount: number;
  latestVersionCode?: number | null;
  latestVersionName?: string | null;
  lastUploadedAt?: string | null;
}

