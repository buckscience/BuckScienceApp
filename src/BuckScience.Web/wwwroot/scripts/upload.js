/**
 * Photo Upload Pipeline - Client-side processing and upload
 * Resizes images to WebP format and uploads directly to Azure Blob Storage
 */

(function() {
    'use strict';

    // Configuration - adjust baseUrl if API is hosted on different origin
    const config = {
        baseUrl: '', // Use relative URLs by default
        targetSizes: {
            thumbnail: { width: 256, height: 256, suffix: '_thumb' },
            display: { width: 1200, height: 932, suffix: '_1200x932' }
        },
        webpQuality: 0.85
    };

    let currentUploads = [];
    let isUploading = false;

    // DOM elements
    let uploadForm, photoFiles, cameraIdInput, uploadBtn, uploadProgress, uploadStatus, uploadResults;

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initializeElements();
        setupEventListeners();
    });

    function initializeElements() {
        uploadForm = document.getElementById('photoUploadForm');
        photoFiles = document.getElementById('photoFiles');
        cameraIdInput = document.getElementById('cameraId');
        uploadBtn = document.getElementById('uploadBtn');
        uploadProgress = document.getElementById('uploadProgress');
        uploadStatus = document.getElementById('uploadStatus');
        uploadResults = document.getElementById('uploadResults');
    }

    function setupEventListeners() {
        if (uploadForm) {
            uploadForm.addEventListener('submit', handleFormSubmit);
        }

        if (photoFiles) {
            photoFiles.addEventListener('change', handleFileSelection);
        }
    }

    function handleFileSelection(event) {
        const files = event.target.files;
        if (files.length > 0) {
            showFileInfo(files);
        }
    }

    function showFileInfo(files) {
        const fileList = Array.from(files).map(file => `${file.name} (${formatFileSize(file.size)})`).join(', ');
        console.log(`Selected ${files.length} file(s): ${fileList}`);
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    async function handleFormSubmit(event) {
        event.preventDefault();
        
        if (isUploading) return;
        
        const files = photoFiles.files;
        const cameraId = parseInt(cameraIdInput.value);
        
        if (!files.length || !cameraId) {
            alert('Please select files and enter a camera ID');
            return;
        }

        try {
            isUploading = true;
            setUploadingState(true);
            showProgress(true);
            
            await processFiles(files, cameraId);
            
        } catch (error) {
            console.error('Upload failed:', error);
            showError(`Upload failed: ${error.message}`);
        } finally {
            isUploading = false;
            setUploadingState(false);
        }
    }

    function setUploadingState(uploading) {
        const spinner = uploadBtn.querySelector('.spinner-border');
        if (uploading) {
            uploadBtn.disabled = true;
            spinner?.classList.remove('d-none');
            uploadBtn.innerHTML = uploadBtn.innerHTML.replace('Upload Photos', 'Uploading...');
        } else {
            uploadBtn.disabled = false;
            spinner?.classList.add('d-none');
            uploadBtn.innerHTML = uploadBtn.innerHTML.replace('Uploading...', 'Upload Photos');
        }
    }

    function showProgress(show) {
        if (show) {
            uploadProgress.classList.remove('d-none');
            updateProgress(0, 'Preparing files...');
        } else {
            uploadProgress.classList.add('d-none');
        }
    }

    function updateProgress(percent, status) {
        const progressBar = uploadProgress.querySelector('.progress-bar');
        progressBar.style.width = percent + '%';
        progressBar.setAttribute('aria-valuenow', percent);
        uploadStatus.textContent = status;
    }

    async function processFiles(files, cameraId) {
        const results = [];
        const totalFiles = files.length;
        
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            const progress = ((i + 1) / totalFiles) * 100;
            updateProgress(progress, `Processing ${file.name} (${i + 1}/${totalFiles})`);
            
            try {
                const result = await processFile(file, cameraId);
                results.push({ file: file.name, success: true, ...result });
            } catch (error) {
                console.error(`Failed to process ${file.name}:`, error);
                results.push({ file: file.name, success: false, error: error.message });
            }
        }
        
        showResults(results);
    }

    async function processFile(file, cameraId) {
        // Calculate content hash
        const arrayBuffer = await file.arrayBuffer();
        const contentHash = await calculateSHA256(arrayBuffer);
        
        // Get user ID (this would normally come from authentication)
        const userId = 'test-user'; // TODO: Get from authenticated context
        
        // Generate blob names
        const thumbBlobName = `${userId}/${contentHash}_thumb.webp`;
        const displayBlobName = `${userId}/${contentHash}_1200x932.webp`;
        
        // Resize and convert to WebP
        const thumbBlob = await resizeImageToWebP(file, config.targetSizes.thumbnail);
        const displayBlob = await resizeImageToWebP(file, config.targetSizes.display);
        
        // Get SAS URLs for both blobs
        const thumbSasUrl = await getSasUrl(userId, thumbBlobName);
        const displaySasUrl = await getSasUrl(userId, displayBlobName);
        
        // Upload both blobs
        await uploadBlob(thumbSasUrl, thumbBlob);
        await uploadBlob(displaySasUrl, displayBlob);
        
        // Register photo
        const registrationData = {
            userId,
            cameraId,
            contentHash,
            thumbBlobName,
            displayBlobName,
            takenAtUtc: new Date().toISOString(), // TODO: Extract from EXIF if available
            latitude: null, // TODO: Extract from EXIF if available
            longitude: null // TODO: Extract from EXIF if available
        };
        
        const photoResult = await registerPhoto(registrationData);
        
        return {
            contentHash,
            thumbBlobName,
            displayBlobName,
            photoId: photoResult.photoId
        };
    }

    async function calculateSHA256(arrayBuffer) {
        const hashBuffer = await crypto.subtle.digest('SHA-256', arrayBuffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    }

    async function resizeImageToWebP(file, targetSize) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            img.onload = function() {
                // Calculate dimensions maintaining aspect ratio
                const { width, height } = calculateDimensions(img.width, img.height, targetSize.width, targetSize.height);
                
                canvas.width = width;
                canvas.height = height;
                
                // Draw and resize
                ctx.drawImage(img, 0, 0, width, height);
                
                // Convert to WebP
                canvas.toBlob(resolve, 'image/webp', config.webpQuality);
            };
            
            img.onerror = reject;
            img.src = URL.createObjectURL(file);
        });
    }

    function calculateDimensions(imgWidth, imgHeight, maxWidth, maxHeight) {
        const ratio = Math.min(maxWidth / imgWidth, maxHeight / imgHeight);
        return {
            width: Math.round(imgWidth * ratio),
            height: Math.round(imgHeight * ratio)
        };
    }

    async function getSasUrl(userId, blobName) {
        const response = await fetch(`${config.baseUrl}/upload/sas`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ userId, blobName })
        });
        
        if (!response.ok) {
            throw new Error(`Failed to get SAS URL: ${response.status} ${response.statusText}`);
        }
        
        const data = await response.json();
        return data.sasUrl;
    }

    async function uploadBlob(sasUrl, blob) {
        const response = await fetch(sasUrl, {
            method: 'PUT',
            headers: {
                'x-ms-blob-type': 'BlockBlob',
                'Content-Type': 'image/webp'
            },
            body: blob
        });
        
        if (!response.ok) {
            throw new Error(`Failed to upload blob: ${response.status} ${response.statusText}`);
        }
    }

    async function registerPhoto(registrationData) {
        const response = await fetch(`${config.baseUrl}/photos/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(registrationData)
        });
        
        if (!response.ok) {
            throw new Error(`Failed to register photo: ${response.status} ${response.statusText}`);
        }
        
        return await response.json();
    }

    function showResults(results) {
        const successCount = results.filter(r => r.success).length;
        const errorCount = results.length - successCount;
        
        let html = `<div class="alert ${errorCount === 0 ? 'alert-success' : 'alert-warning'}" role="alert">`;
        html += `<h5>Upload Complete</h5>`;
        html += `<p>Successfully processed ${successCount} of ${results.length} files.</p>`;
        html += `</div>`;
        
        if (results.length > 0) {
            html += `<div class="table-responsive">`;
            html += `<table class="table table-sm">`;
            html += `<thead><tr><th>File</th><th>Status</th><th>Details</th></tr></thead>`;
            html += `<tbody>`;
            
            results.forEach(result => {
                html += `<tr class="${result.success ? 'table-success' : 'table-danger'}">`;
                html += `<td>${escapeHtml(result.file)}</td>`;
                html += `<td><span class="badge ${result.success ? 'bg-success' : 'bg-danger'}">${result.success ? 'Success' : 'Failed'}</span></td>`;
                html += `<td>`;
                if (result.success) {
                    html += `Photo ID: ${result.photoId}<br>Hash: ${result.contentHash.substring(0, 8)}...`;
                } else {
                    html += `Error: ${escapeHtml(result.error)}`;
                }
                html += `</td>`;
                html += `</tr>`;
            });
            
            html += `</tbody></table></div>`;
        }
        
        uploadResults.innerHTML = html;
    }

    function showError(message) {
        uploadResults.innerHTML = `<div class="alert alert-danger" role="alert">${escapeHtml(message)}</div>`;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

})();