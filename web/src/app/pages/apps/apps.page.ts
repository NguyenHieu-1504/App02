import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AppSummary } from '../../models/app-summary.model';
import { ApkApiService } from '../../services/apk-api.service';

@Component({
  selector: 'app-apps-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './apps.page.html'
})
export class AppsPage implements OnInit {
  loading = true;
  error: string | null = null;
  apps: AppSummary[] = [];
  private loadSeq = 0;

  constructor(
    private readonly api: ApkApiService,
    private readonly cdr: ChangeDetectorRef
  ) { }

  async ngOnInit() {
    await this.reload();
  }

  async reload() {
    const seq = ++this.loadSeq;
    this.loading = true;
    this.error = null;
    try {
      const result = await this.api.listApps();
      if (seq !== this.loadSeq) return;
      this.apps = result;
    } catch (e: any) {
      if (seq !== this.loadSeq) return;
      this.error = e?.error?.error ?? e?.message ?? 'Không thể tải danh sách ứng dụng.';
    } finally {
      if (seq === this.loadSeq) {
        this.loading = false;
        this.cdr.detectChanges();
      }
    }
  }
}
