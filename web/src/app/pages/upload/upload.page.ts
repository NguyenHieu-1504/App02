import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';

import { ApkApiService } from '../../services/apk-api.service';
import { ApkRelease } from '../../models/apk-release.model';

@Component({
  selector: 'app-upload-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './upload.page.html'
})
export class UploadPage {
  selectedFile: File | null = null;
  isUploading = false;
  error: string | null = null;
  success: ApkRelease | null = null;

  constructor(
    private readonly api: ApkApiService,
    private readonly cdr: ChangeDetectorRef
  ) { }

  onFileSelected(files: FileList | null) {
    this.error = null;
    this.success = null;
    this.selectedFile = files?.item(0) ?? null;
  }

  async upload() {
    this.error = null;
    this.success = null;
    if (!this.selectedFile) {
      this.error = 'Vui lòng chọn file .apk.';
      return;
    }
    if (!this.selectedFile.name.toLowerCase().endsWith('.apk')) {
      this.error = 'Chỉ chấp nhận file .apk.';
      return;
    }

    this.isUploading = true;
    try {
      this.success = await this.api.uploadApk(this.selectedFile);
      this.selectedFile = null;
    } catch (e: any) {
      const status = e?.status;
      if (status === 409) {
        this.error = 'Version đã tồn tại (trùng packageName + versionCode). Hãy tăng versionCode hoặc xóa bản cũ trước khi upload lại.';
      } else if (status === 401) {
        this.error = 'Không đủ quyền upload. Kiểm tra API key trong trình duyệt (localStorage.apkApiKey).';
      } else {
        this.error = e?.error?.error ?? e?.message ?? 'Upload thất bại.';
      }
    } finally {
      this.isUploading = false;
      this.cdr.detectChanges();
    }
  }
}
