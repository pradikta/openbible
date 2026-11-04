(window => {
    'use strict';

    window.openBibleFiles = {
        downloadText: (fileName, content, mimeType) => {
            const url = URL.createObjectURL(new Blob([content], { type: mimeType }));
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        }
    };
})(window);
