import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';

import { ApkRelease } from '../../models/apk-release.model';
import { ApkApiService } from '../../services/apk-api.service';

type SortBy = 'versionCode' | 'uploadedAt';
type Order = 'asc' | 'desc';

@Component({
  selector: 'app-versions-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './versions.page.html'
})
export class VersionsPage implements OnInit, OnDestroy {
  private sub?: Subscription;
  private loadSeq = 0;

  packageName = '';
  sort: SortBy = 'versionCode';
  order: Order = 'desc';

  loading = true;
  error: string | null = null;
  versions: ApkRelease[] = [];
  deleting: Record<number, boolean> = {};

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApkApiService,
    private readonly cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.sub = combineLatest([this.route.paramMap, this.route.queryParamMap]).subscribe(([params, query]) => {
      this.packageName = params.get('packageName') ?? query.get('package') ?? '';
      void this.reload();
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }

  async reload() {
    const seq = ++this.loadSeq;
    this.loading = true;
    this.error = null;

    if (!this.packageName) {
      this.versions = [];
      this.error = 'Không tìm thấy packageName. Vui lòng mở lại từ trang Ứng dụng.';
      this.loading = false;
      this.cdr.detectChanges();
      return;
    }

    try {
      const result = await this.api.listVersions(this.packageName, this.sort, this.order);
      if (seq !== this.loadSeq) return;
      this.versions = result;
    } catch (e: any) {
      if (seq !== this.loadSeq) return;
      this.error = e?.error?.error ?? e?.message ?? 'Không thể tải danh sách phiên bản.';
      this.versions = [];
    } finally {
      if (seq === this.loadSeq) {
        this.loading = false;
        this.cdr.detectChanges();
      }
    }
  }

  setSort(sort: SortBy, order: Order) {
    this.sort = sort;
    this.order = order;
    void this.reload();
  }

  downloadUrl(v: ApkRelease) {
    return this.api.downloadUrl(v.packageName, v.versionCode);
  }

  formatSize(bytes: number): string {
    const mb = bytes / 1024 / 1024;
    return `${mb.toFixed(2)} MB`;
  }

  async delete(v: ApkRelease) {
    const ok = confirm(`Xóa APK ${v.packageName} (versionCode: ${v.versionCode})? Hành động này không thể hoàn tác.`);
    if (!ok) return;

    this.deleting[v.versionCode] = true;
    this.error = null;
    try {
      await this.api.deleteVersion(v.packageName, v.versionCode);
      await this.reload();
    } catch (e: any) {
      this.error = e?.error?.error ?? e?.message ?? 'Xóa thất bại.';
    } finally {
      this.deleting[v.versionCode] = false;
      this.cdr.detectChanges();
    }
  }
}
