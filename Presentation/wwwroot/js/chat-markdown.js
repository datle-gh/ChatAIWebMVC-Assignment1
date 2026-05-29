(function () {
    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function renderInline(value) {
        return escapeHtml(value)
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
            .replace(/__([^_]+)__/g, '<strong>$1</strong>')
            .replace(/\*([^*\n]+)\*/g, '<em>$1</em>')
            .replace(/_([^_\n]+)_/g, '<em>$1</em>')
            .replace(/\n/g, '<br>');
    }

    function renderMarkdown(value) {
        const lines = String(value ?? '').replace(/\r\n/g, '\n').split('\n');
        let html = '';
        let paragraph = [];
        let inOrderedList = false;
        let inUnorderedList = false;
        let inCodeBlock = false;
        let codeLines = [];

        function closeLists() {
            if (inOrderedList) {
                html += '</ol>';
                inOrderedList = false;
            }
            if (inUnorderedList) {
                html += '</ul>';
                inUnorderedList = false;
            }
        }

        function flushParagraph() {
            if (paragraph.length === 0) return;
            closeLists();
            html += `<p>${renderInline(paragraph.join('\n'))}</p>`;
            paragraph = [];
        }

        function flushCodeBlock() {
            html += `<pre><code>${escapeHtml(codeLines.join('\n'))}</code></pre>`;
            codeLines = [];
        }

        for (const rawLine of lines) {
            const line = rawLine.trimEnd();
            const trimmed = line.trim();

            if (trimmed.startsWith('```')) {
                if (inCodeBlock) {
                    flushCodeBlock();
                    inCodeBlock = false;
                } else {
                    flushParagraph();
                    closeLists();
                    inCodeBlock = true;
                }
                continue;
            }

            if (inCodeBlock) {
                codeLines.push(rawLine);
                continue;
            }

            if (!trimmed) {
                flushParagraph();
                closeLists();
                continue;
            }

            const heading = trimmed.match(/^(#{1,4})\s+(.+)$/);
            if (heading) {
                flushParagraph();
                closeLists();
                const level = Math.min(heading[1].length + 2, 6);
                html += `<h${level}>${renderInline(heading[2])}</h${level}>`;
                continue;
            }

            const orderedItem = trimmed.match(/^(\d+)\.\s+(.+)$/);
            if (orderedItem) {
                flushParagraph();
                if (inUnorderedList) {
                    html += '</ul>';
                    inUnorderedList = false;
                }
                if (!inOrderedList) {
                    html += '<ol>';
                    inOrderedList = true;
                }
                html += `<li>${renderInline(orderedItem[2])}</li>`;
                continue;
            }

            const unorderedItem = trimmed.match(/^[-*]\s+(.+)$/);
            if (unorderedItem) {
                flushParagraph();
                if (inOrderedList) {
                    html += '</ol>';
                    inOrderedList = false;
                }
                if (!inUnorderedList) {
                    html += '<ul>';
                    inUnorderedList = true;
                }
                html += `<li>${renderInline(unorderedItem[1])}</li>`;
                continue;
            }

            paragraph.push(line);
        }

        if (inCodeBlock) {
            flushCodeBlock();
        }
        flushParagraph();
        closeLists();

        return html;
    }

    function renderElement(element) {
        element.classList.add('chat-message-markdown');
        element.innerHTML = renderMarkdown(element.textContent || '');
    }

    function renderAll(root) {
        (root || document)
            .querySelectorAll('[data-render-markdown="true"]')
            .forEach(renderElement);
    }

    window.ChatMarkdown = {
        render: renderMarkdown,
        renderAll
    };
})();
