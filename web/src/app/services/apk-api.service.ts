import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AppSummary } from '../models/app-summary.model';
import { ApkRelease } from '../models/apk-release.model';

@Injectable({ providedIn: 'root' })
export class ApkApiService {
  private static readonly defaultApiKey = 'dev-local-key';

  constructor(private readonly http: HttpClient) {}

  async uploadApk(file: File): Promise<ApkRelease> {
    const fd = new FormData();
    fd.append('file', file);
    return await firstValueFrom(this.http.post<ApkRelease>('/api/apk/upload', fd, { headers: this.authHeaders() }));
  }

  async listApps(): Promise<AppSummary[]> {
    const params = new HttpParams().set('_ts', Date.now().toString());
    return await firstValueFrom(this.http.get<AppSummary[]>('/api/apps', { params }));
  }

  async listVersions(
    packageName: string,
    sort: 'versionCode' | 'uploadedAt' = 'versionCode',
    order: 'asc' | 'desc' = 'desc'
  ): Promise<ApkRelease[]> {
    const params = new HttpParams()
      .set('sort', sort)
      .set('order', order)
      .set('_ts', Date.now().toString());
    return await firstValueFrom(this.http.get<ApkRelease[]>(`/api/apps/${encodeURIComponent(packageName)}/versions`, { params }));
  }

  downloadUrl(packageName: string, versionCode: number): string {
    const apiKey = encodeURIComponent(this.getApiKey());
    return `/api/apk/download/${encodeURIComponent(packageName)}/${versionCode}?apiKey=${apiKey}`;
  }

  async deleteVersion(packageName: string, versionCode: number): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`/api/apk/${encodeURIComponent(packageName)}/${versionCode}`, { headers: this.authHeaders() }));
  }

  private getApiKey(): string {
    if (typeof window === 'undefined') return ApkApiService.defaultApiKey;
    return localStorage.getItem('apkApiKey') || ApkApiService.defaultApiKey;
  }

  private authHeaders(): Record<string, string> {
    return { 'X-Api-Key': this.getApiKey() };
  }
}

