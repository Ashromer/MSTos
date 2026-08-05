document.addEventListener('DOMContentLoaded', () => {
    // Initialize Lucide Icons
    if (window.lucide) {
        window.lucide.createIcons();
    }

    // --- ALTURA REAL DEL HEADER FIJO ---
    // El header es fixed y opaco: todo lo que entra por arriba (heroes, indices,
    // bloques de intro) necesita reservar exactamente su alto. Medirlo evita el
    // numero magico de 120px, que sobra en movil y se queda corto en pantallas grandes.
    const siteHeader = document.querySelector('header');
    function syncHeaderHeight() {
        if (!siteHeader) return;
        // Al hacer scroll el header se encoge y se vuelve transparente: hay que medir
        // SIEMPRE el estado desplegado, que es el que tapa la parte alta de la pagina.
        const wasScrolled = siteHeader.classList.contains('scrolled');
        if (wasScrolled) siteHeader.classList.remove('scrolled');
        const h = Math.round(siteHeader.getBoundingClientRect().height);
        if (wasScrolled) siteHeader.classList.add('scrolled');
        if (h > 0) document.documentElement.style.setProperty('--header-h', h + 'px');
    }
    syncHeaderHeight();
    window.addEventListener('resize', syncHeaderHeight);
    window.addEventListener('load', syncHeaderHeight);

    // --- CUSTOM MINIMAL CURSOR ---
    const cursor = document.querySelector('.custom-cursor');
    const follower = document.querySelector('.custom-cursor-follower');
    // Only hide the native cursor on fine pointers once the custom one exists
    if (cursor && follower && window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
        document.documentElement.classList.add('cursor-ready');
    }
    let mouseX = 0, mouseY = 0;
    let cursorX = 0, cursorY = 0;
    let followerX = 0, followerY = 0;

    document.addEventListener('mousemove', (e) => {
        mouseX = e.clientX;
        mouseY = e.clientY;
        
        if (cursor && follower) {
            cursor.style.opacity = '1';
            follower.style.opacity = '1';
        }
    });

    function animateCursor() {
        cursorX += (mouseX - cursorX) * 0.25;
        cursorY += (mouseY - cursorY) * 0.25;
        
        followerX += (mouseX - followerX) * 0.12;
        followerY += (mouseY - followerY) * 0.12;

        if (cursor) {
            cursor.style.left = `${cursorX}px`;
            cursor.style.top = `${cursorY}px`;
        }
        if (follower) {
            follower.style.left = `${followerX}px`;
            follower.style.top = `${followerY}px`;
        }

        requestAnimationFrame(animateCursor);
    }
    animateCursor();

    // Hover elements expansion
    function refreshCursorHoverListeners() {
        const hoverables = document.querySelectorAll(
            'a, button, .console-btn, .selector-panel, .platform-btn, .contact-link-item, .block-arrow, .info-trigger, .info-close-btn, .mobile-nav-toggle, .nav-link-btn, .nav-strip-item'
        );
        hoverables.forEach(el => {
            el.removeEventListener('mouseenter', addCursorHover);
            el.removeEventListener('mouseleave', removeCursorHover);
            el.addEventListener('mouseenter', addCursorHover);
            el.addEventListener('mouseleave', removeCursorHover);
        });
    }

    function addCursorHover() {
        if (cursor && follower) {
            cursor.classList.add('hovered');
            follower.classList.add('hovered');
        }
    }

    function removeCursorHover() {
        if (cursor && follower) {
            cursor.classList.remove('hovered');
            follower.classList.remove('hovered');
        }
    }

    refreshCursorHoverListeners();

    // --- TAB SYSTEM & LANDING SELECTOR INTEGRATION ---
    const landingSelector = document.getElementById('landing-selector');
    const mainWrapperSite = document.getElementById('main-wrapper-site');
    const tabPanels = document.querySelectorAll('.tab-panel');
    const navLinkButtons = document.querySelectorAll('.nav-link-btn');
    const logoBack = document.getElementById('logo-back');

    // Landing Panel Clicks
    const selectorPanels = document.querySelectorAll('.selector-panel');
    selectorPanels.forEach(panel => {
        panel.addEventListener('click', () => {
            const targetTab = panel.getAttribute('data-tab');
            switchTab(targetTab);
            
            // Hide landing, show main site with animation
            landingSelector.classList.add('hidden');
            mainWrapperSite.classList.remove('hidden');
            
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    });

    // Nav Bar Tab Clicks
    navLinkButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            const targetTab = btn.getAttribute('data-target');
            if (targetTab) {
                switchTab(targetTab);
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
        });
    });

    // Tech "Tell me your workflow" CTA -> Contact, pre-selecting the BIM/code interest
    document.querySelectorAll('[data-go-contact]').forEach(btn => {
        btn.addEventListener('click', () => {
            switchTab('contact-page');
            const typeSelect = document.getElementById('form-type');
            if (typeSelect) typeSelect.value = 'tech';
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    });

    // Back to Landing Selector on Logo Click
    if (logoBack) {
        logoBack.addEventListener('click', (e) => {
            e.preventDefault();
            
            // Fade out main, fade in landing
            mainWrapperSite.classList.add('hidden');
            landingSelector.classList.remove('hidden');
            
            // Reset active tabs
            navLinkButtons.forEach(b => b.classList.remove('active'));
            tabPanels.forEach(p => p.classList.remove('active'));
        });
    }

    function switchTab(tabId) {
        // Toggle Nav Buttons Active Class
        navLinkButtons.forEach(btn => {
            if (btn.getAttribute('data-target') === tabId) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });

        // Toggle Tab Panels Active Class
        tabPanels.forEach(panel => {
            if (panel.id === `panel-${tabId}`) {
                panel.classList.add('active');
            } else {
                panel.classList.remove('active');
            }
        });

        // Close any open info panels in all tabs
        document.querySelectorAll('.block-info-panel.active').forEach(p => {
            p.classList.remove('active');
        });

        // Scroll-snap por proyecto solo en architecture/visualization (no romper páginas de texto)
        // Visualization keeps full-bleed snap; Architecture is moving to an
        // editorial (El Croquis-style) scroll, so it no longer snaps.
        if (tabId === 'visualization') {
            document.documentElement.classList.add('snap-mode');
        } else {
            document.documentElement.classList.remove('snap-mode');
        }

        // Entering Architecture always lands on the project index
        if (tabId === 'architecture') {
            document.querySelectorAll('.arch-view.active').forEach(v => v.classList.remove('active'));
            const ai = document.getElementById('arch-index');
            if (ai) ai.classList.remove('hidden');
        }

        // Replay the CNC toolpath draw each time Technology becomes visible
        if (tabId === 'technology') {
            const sig = document.querySelector('.tech-signature');
            if (sig) {
                sig.classList.remove('animate');
                void sig.offsetWidth; // force reflow so the animation restarts
                sig.classList.add('animate');
            }
        }

        if (window.lucide) {
            window.lucide.createIcons();
        }
        refreshCursorHoverListeners();

        // la primera vez que se entra, la primera tarjeta hace un guino
        if (tabId === 'architecture' && window.__hintFirstLogo) window.__hintFirstLogo('panel-architecture');
        if (tabId === 'visualization' && window.__hintFirstLogo) window.__hintFirstLogo('panel-visualization');
    }


    // --- LANGUAGE SWITCHER SYSTEM ---
    const langButtons = document.querySelectorAll('.lang-btn');
    
    langButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const lang = btn.getAttribute('data-lang');
            setLanguage(lang);
        });
    });

    function setLanguage(lang) {
        if (lang === 'en') {
            document.body.classList.remove('lang-es');
            document.body.classList.add('lang-en');
        } else {
            document.body.classList.remove('lang-en');
            document.body.classList.add('lang-es');
        }
        document.documentElement.lang = (lang === 'en') ? 'en' : 'es';

        langButtons.forEach(b => {
            if (b.getAttribute('data-lang') === lang) {
                b.classList.add('active');
            } else {
                b.classList.remove('active');
            }
        });

        // <option> no admite hijos: traducir su texto por atributo data-es/data-en
        document.querySelectorAll('#form-type option').forEach(opt => {
            const txt = opt.getAttribute('data-' + lang);
            if (txt) opt.textContent = txt;
        });

        localStorage.setItem('mst_portfolio_lang', lang);
        
        if (window.lucide) {
            window.lucide.createIcons();
        }
        refreshCursorHoverListeners();
    }

    // Idioma inicial: manda lo que el visitante eligio a mano; si nunca ha elegido,
    // se mira el idioma del NAVEGADOR (no la IP: no hace falta servicio externo, no
    // hay latencia ni datos de terceros, y acierta mas — un espanol en Londres sigue
    // leyendo en castellano). Si no queda claro, ingles.
    function detectLang() {
        const list = navigator.languages && navigator.languages.length
            ? navigator.languages
            : [navigator.language || ''];
        return list.some(l => String(l).toLowerCase().startsWith('es')) ? 'es' : 'en';
    }
    const savedLang = localStorage.getItem('mst_portfolio_lang') || detectLang();
    setLanguage(savedLang);


    // --- MOBILE NAV TOGGLE ---
    const mobileToggle = document.querySelector('.mobile-nav-toggle');
    const navLinks = document.querySelector('.nav-links');

    if (mobileToggle && navLinks) {
        // lucide.createIcons() reemplaza el <i> por un <svg>, por lo que querySelector('i')
        // devuelve null tras el primer render. Reconstruir el <i> es robusto en cada toggle.
        function setToggleIcon(name) {
            mobileToggle.innerHTML = `<i data-lucide="${name}"></i>`;
            if (window.lucide) {
                window.lucide.createIcons();
            }
        }

        mobileToggle.addEventListener('click', () => {
            navLinks.classList.toggle('active');
            setToggleIcon(navLinks.classList.contains('active') ? 'x' : 'menu');
        });

        // Close links
        navLinks.querySelectorAll('.nav-link-btn').forEach(lnk => {
            lnk.addEventListener('click', () => {
                navLinks.classList.remove('active');
                setToggleIcon('menu');
            });
        });
    }


    // --- MOCK SCRIPTS DATABASE FOR THE BIM CONSOLE ---
    const consoleBody = document.getElementById('console-body');
    const consoleButtons = document.querySelectorAll('.console-btn');
    let isTyping = false;

    const consoleScripts = {
        'metalperfil': {
            cmd: 'run metalperfil_cad_sync.py --staggered-snake --safety-check',
            lines: [
                { type: 'output', text: 'Iniciando lectura de bases de datos Metalperfil...' },
                { type: 'output', text: 'Cargando reglas y catalogo de diámetros desde config_machines.json...' },
                { type: 'output', text: 'Ejecutando validación de fabricabilidad (ligamentos y diámetros máximos)...' },
                { type: 'success', text: 'COMPATIBILIDAD APROBADA: Distancia de centros y ligamentos seguros (OK).' },
                { type: 'output', text: 'Calculando coordenadas de taladrado optimizadas...' },
                { type: 'output', text: 'Aplicando optimización Snake-Path (zig-zag continuo de cabezal CNC)...' },
                { type: 'output', text: 'Exportando perforaciones como entidades CIRCLE a DXF R12 en ASCII plano...' },
                { type: 'success', text: 'Automatización completada. Archivo paneles_perforados.dxf generado con éxito.' }
            ]
        },
        'canopy': {
            cmd: 'run openai_dynamo_facade.py --iterations=5 --brightness-mapping',
            lines: [
                { type: 'output', text: 'Leyendo descomposición geométrica de la marquesina en Revit 2023/2024...' },
                { type: 'output', text: 'Enviando petición a la API de OpenAI (Módulo Da-Vinci/DALL-E)...' },
                { type: 'output', text: 'Muestreando mapas de bits NumPy de patrones naturales generados por IA...' },
                { type: 'output', text: 'Calculando espesores físicos proporcionales a la luminancia por pixel...' },
                { type: 'success', text: 'Sólidos variables generados y mapeados a los paneles de fachada en Revit.' }
            ]
        },
        'audit': {
            cmd: 'run revit_to_presto.py --shared-params --export-excel',
            lines: [
                { type: 'output', text: 'Conectando con base de datos del modelo activo...' },
                { type: 'output', text: 'Buscando parámetros compartidos CIP_CodigoUO, CIP_Capitulo y CIP_Unidad...' },
                { type: 'output', text: 'Calculando automáticamente volumen (m3) e inyectando medición...' },
                { type: 'output', text: 'Procesando conductos MEP, muros y cimentaciones del proyecto...' },
                { type: 'success', text: 'Model Audit & Estimación completada. 0 colisiones críticas. Exportado a Presto.' }
            ]
        }
    };

    consoleButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const scriptName = btn.getAttribute('data-run');
            if (isTyping || !consoleScripts[scriptName]) return;
            triggerConsole(consoleScripts[scriptName]);
        });
    });

    function triggerConsole(script) {
        isTyping = true;
        consoleBody.innerHTML = '';
        
        let lineIdx = 0;
        
        function drawNext() {
            if (lineIdx >= script.lines.length) {
                const prompt = document.createElement('div');
                prompt.className = 'console-line';
                prompt.innerHTML = `<span class="c-prompt">C:\\RevitPythonShell></span> <span class="console-cursor"></span>`;
                consoleBody.appendChild(prompt);
                consoleBody.scrollTop = consoleBody.scrollHeight;
                isTyping = false;
                return;
            }

            const step = script.lines[lineIdx];
            
            if (lineIdx === 0) {
                const cmdLine = document.createElement('div');
                cmdLine.className = 'console-line';
                cmdLine.innerHTML = `<span class="c-prompt">C:\\RevitPythonShell></span> <span class="cmd-text"></span><span class="console-cursor"></span>`;
                consoleBody.appendChild(cmdLine);
                const txtSpan = cmdLine.querySelector('.cmd-text');
                const curSpan = cmdLine.querySelector('.console-cursor');
                
                let charIdx = 0;
                const typeTimer = setInterval(() => {
                    if (charIdx < script.cmd.length) {
                        txtSpan.textContent += script.cmd.charAt(charIdx);
                        charIdx++;
                    } else {
                        clearInterval(typeTimer);
                        curSpan.remove();
                        lineIdx++;
                        setTimeout(drawNext, 200);
                    }
                }, 20);
            } else {
                const outLine = document.createElement('div');
                if (step.type === 'success') {
                    outLine.className = 'console-line c-success';
                } else {
                    outLine.className = 'console-line c-output';
                }
                outLine.textContent = step.text;
                consoleBody.appendChild(outLine);
                consoleBody.scrollTop = consoleBody.scrollHeight;
                lineIdx++;
                setTimeout(drawNext, step.type === 'success' ? 400 : 150);
            }
        }

        drawNext();
    }


    // --- PLAY-TIME.ES STYLE INLINE SLIDER SYSTEM ---
    const projectBlocks = document.querySelectorAll('.project-section-block');
    let activeBlock = null;

    projectBlocks.forEach(block => {
        const sliderWrapper = block.querySelector('.slider-wrapper');
        const slides = block.querySelectorAll('.slide-item');
        const arrowLeft = block.querySelector('.arrow-left');
        const arrowRight = block.querySelector('.arrow-right');
        
        // Info panel elements
        const infoPanel = block.querySelector('.block-info-panel');
        const infoClose = block.querySelector('.info-close-btn');

        if (!sliderWrapper || slides.length === 0) return;

        let currentSlide = 0;
        const totalSlides = slides.length;

        // Remove old text indicator
        const oldIndicator = block.querySelector('.block-slide-indicator');
        if (oldIndicator) {
            oldIndicator.remove();
        }

        const title = block.querySelector('.caption-title');
        const dotsContainer = block.querySelector('.block-slide-dots');

        if (title && infoPanel) {
            // Add click listener on centered title to toggle info panel
            title.addEventListener('click', (e) => {
                e.stopPropagation();
                infoPanel.classList.toggle('active');
            });
        }

        // Generate slide dots
        if (dotsContainer) {
            dotsContainer.innerHTML = '';
            for (let i = 0; i < totalSlides; i++) {
                const dot = document.createElement('span');
                dot.className = 'slide-dot' + (i === 0 ? ' active' : '');
                dot.addEventListener('click', (e) => {
                    e.stopPropagation();
                    currentSlide = i;
                    updateBlockSlider();
                });
                dotsContainer.appendChild(dot);
            }

            // Hide dots if there's only 1 slide
            if (totalSlides <= 1) {
                dotsContainer.style.display = 'none';
            }
        }

        // Hide arrows if there's only 1 slide
        if (totalSlides <= 1) {
            if (arrowLeft) arrowLeft.style.display = 'none';
            if (arrowRight) arrowRight.style.display = 'none';
        }

        function updateArrowVisibility() {
            // Permanent arrows: only hide the one pointing where there is no slide
            if (arrowLeft) arrowLeft.classList.toggle('edge-hidden', currentSlide === 0);
            if (arrowRight) arrowRight.classList.toggle('edge-hidden', currentSlide === totalSlides - 1);
        }
        updateArrowVisibility();

        function updateBlockSlider() {
            // Apply horizontal sliding shift
            sliderWrapper.style.transform = `translateX(-${currentSlide * 100}%)`;

            updateArrowVisibility();

            // Update active dot style
            if (dotsContainer) {
                const dots = dotsContainer.querySelectorAll('.slide-dot');
                dots.forEach((dot, idx) => {
                    if (idx === currentSlide) {
                        dot.classList.add('active');
                    } else {
                        dot.classList.remove('active');
                    }
                });
            }

            // Close the slide-up info panel whenever slide changes
            if (infoPanel) {
                infoPanel.classList.remove('active');
            }
        }

        // Left Arrow Click
        if (arrowLeft) {
            arrowLeft.addEventListener('click', (e) => {
                e.stopPropagation();
                currentSlide = Math.max(currentSlide - 1, 0);
                updateBlockSlider();
            });
        }

        // Right Arrow Click
        if (arrowRight) {
            arrowRight.addEventListener('click', (e) => {
                e.stopPropagation();
                currentSlide = Math.min(currentSlide + 1, totalSlides - 1);
                updateBlockSlider();
            });
        }

        // Open Info Panel event listener is no longer needed here as title handles toggling.

        // Close Info Panel
        if (infoClose && infoPanel) {
            infoClose.addEventListener('click', (e) => {
                e.stopPropagation();
                infoPanel.classList.remove('active');
            });
        }

        // Support swipe gestures on touch screens for intuitive mobile control
        let touchStartX = 0;
        let touchEndX = 0;

        block.addEventListener('touchstart', (e) => {
            touchStartX = e.changedTouches[0].screenX;
        }, { passive: true });

        block.addEventListener('touchend', (e) => {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        }, { passive: true });

        function handleSwipe() {
            const threshold = 50; // pixels
            const diffX = touchEndX - touchStartX;
            if (Math.abs(diffX) > threshold) {
                if (diffX < 0) {
                    // Swipe left -> next slide
                    currentSlide = Math.min(currentSlide + 1, totalSlides - 1);
                    updateBlockSlider();
                } else {
                    // Swipe right -> prev slide
                    currentSlide = Math.max(currentSlide - 1, 0);
                    updateBlockSlider();
                }
            }
        }

        // Attach custom slider controls to block object so we can invoke them externally via keyboard
        block.sliderMethods = {
            next: () => {
                currentSlide = Math.min(currentSlide + 1, totalSlides - 1);
                updateBlockSlider();
            },
            prev: () => {
                currentSlide = Math.max(currentSlide - 1, 0);
                updateBlockSlider();
            },
            closeInfo: () => {
                if (infoPanel) infoPanel.classList.remove('active');
            }
        };
    });

    // Refresh cursor hover listeners once project blocks are initialized
    refreshCursorHoverListeners();

    // --- KEYBOARD CONTROLS FOR ACTIVE VIEWPORT BLOCK ---
    // Use IntersectionObserver to determine which project section is currently visible to the user
    const observerOptions = {
        root: null,
        rootMargin: '-25% 0px -25% 0px', // check elements taking up center viewport space
        threshold: 0.2
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                activeBlock = entry.target;
            }
        });
    }, observerOptions);

    projectBlocks.forEach(block => {
        observer.observe(block);
    });

    // Handle physical keyboard shortcuts (ArrowLeft, ArrowRight, Escape)
    document.addEventListener('keydown', (e) => {
        // Only run shortcuts if the selector page is hidden (i.e. user is inside portfolio content tabs)
        if (landingSelector.classList.contains('hidden') && activeBlock && activeBlock.sliderMethods) {
            if (e.key === 'ArrowRight') {
                activeBlock.sliderMethods.next();
            } else if (e.key === 'ArrowLeft') {
                activeBlock.sliderMethods.prev();
            } else if (e.key === 'Escape') {
                activeBlock.sliderMethods.closeInfo();
            }
        }
    });

    // --- CONTACT FORM (Web3Forms async send + mailto fallback) ---
    // Pega aquí tu Access Key gratuita de https://web3forms.com (verificación por email).
    // Mientras esté vacía, el formulario abre el cliente de correo como respaldo.
    const WEB3FORMS_ACCESS_KEY = '';
    const CONTACT_EMAIL = 'miguel.suarez.torres.25@gmail.com';

    const contactForm = document.getElementById('contact-form');
    if (contactForm) {
        const statusEl = document.createElement('p');
        statusEl.className = 'form-status';
        statusEl.setAttribute('role', 'status');
        statusEl.setAttribute('aria-live', 'polite');
        contactForm.appendChild(statusEl);
        const submitBtn = contactForm.querySelector('.submit-form-btn');

        const t = (es, en) => document.body.classList.contains('lang-en') ? en : es;

        contactForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const isEN = document.body.classList.contains('lang-en');
            const name = (document.getElementById('form-name').value || '').trim();
            const email = (document.getElementById('form-email').value || '').trim();
            const typeSel = document.getElementById('form-type');
            const typeOpt = typeSel.options[typeSel.selectedIndex];
            const area = typeOpt.getAttribute(isEN ? 'data-en' : 'data-es') || typeOpt.text;
            const message = (document.getElementById('form-message').value || '').trim();
            const subject = `[Portfolio] ${area} — ${name}`;

            const openMailto = () => {
                const body =
                    (isEN ? 'Name: ' : 'Nombre: ') + name + '\n' +
                    (isEN ? 'Email: ' : 'Correo: ') + email + '\n' +
                    (isEN ? 'Area: ' : 'Área: ') + area + '\n\n' + message;
                window.location.href = 'mailto:' + CONTACT_EMAIL
                    + '?subject=' + encodeURIComponent(subject)
                    + '&body=' + encodeURIComponent(body);
            };

            // Sin clave configurada → respaldo mailto inmediato
            if (!WEB3FORMS_ACCESS_KEY) {
                statusEl.className = 'form-status';
                statusEl.textContent = t('Abriendo tu cliente de correo…', 'Opening your email client…');
                openMailto();
                return;
            }

            statusEl.className = 'form-status';
            statusEl.textContent = t('Enviando…', 'Sending…');
            if (submitBtn) submitBtn.disabled = true;

            try {
                const res = await fetch('https://api.web3forms.com/submit', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                    body: JSON.stringify({
                        access_key: WEB3FORMS_ACCESS_KEY,
                        subject, name, email,
                        area, message,
                        from_name: name,
                        replyto: email
                    })
                });
                const data = await res.json();
                if (data.success) {
                    statusEl.className = 'form-status is-ok';
                    statusEl.textContent = t('¡Mensaje enviado! Te responderé pronto.', 'Message sent! I\'ll get back to you soon.');
                    contactForm.reset();
                } else {
                    throw new Error(data.message || 'send failed');
                }
            } catch (err) {
                statusEl.className = 'form-status is-err';
                statusEl.textContent = t('No se pudo enviar. Abriendo tu correo…', 'Couldn\'t send. Opening your email…');
                openMailto();
            } finally {
                if (submitBtn) submitBtn.disabled = false;
            }
        });
    }

    // --- ARCHITECTURE: index <-> project view router (no infinite scroll) ---
    const archIndex = document.getElementById('arch-index');
    const archViews = document.querySelectorAll('.arch-view');

    function openArchProject(slug) {
        archViews.forEach(v => v.classList.toggle('active', v.getAttribute('data-arch-view') === slug));
        if (archIndex) archIndex.classList.add('hidden');
        window.scrollTo({ top: 0, behavior: 'auto' });
        if (window.lucide) window.lucide.createIcons();
        refreshCursorHoverListeners();
    }

    function closeArchProject() {
        archViews.forEach(v => v.classList.remove('active'));
        if (archIndex) archIndex.classList.remove('hidden');
        window.scrollTo({ top: 0, behavior: 'auto' });
    }

    document.querySelectorAll('[data-arch-open]').forEach(btn => {
        btn.addEventListener('click', () => openArchProject(btn.getAttribute('data-arch-open')));
    });
    document.querySelectorAll('[data-arch-close]').forEach(btn => {
        btn.addEventListener('click', closeArchProject);
    });

    // --- PORTADA: la imagen de cada panel va rotando ---
    // Solo se usan imagenes que YA estan referenciadas en el HTML, para que sigan
    // subiendo con publicar_imagenes.ps1 y no haya que mantener una lista aparte.
    const PANEL_BGS = {
        architecture: [
            'content/arquitectura/lighthouse/05_Hero%20shot.jpg',
            'content/arquitectura/orkide/00_Inicio.jpg',
            'content/arquitectura/waraqa/5_Fotomontaje%201_enhanced.jpg',
            'content/arquitectura/barbate/01.jpg'
        ],
        visualization: [
            'content/visualizacion/sem/01.jpg',
            'content/visualizacion/caixaforum/01.jpg',
            'content/visualizacion/kaira-looro/01.jpg',
            'content/visualizacion/tec/01.jpg',
            'content/visualizacion/csic/02.jpg'
        ],
        technology: [
            'content/tech/cnc-snake-path/01.jpg',
            'content/tech/metalperfil/01.jpg',
            'content/tech/canopy-ia/01.jpg',
            'content/tech/dynamo-suite/01.jpg'
        ]
    };

    (function rotatePanelBackgrounds() {
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

        document.querySelectorAll('.selector-panel').forEach(panel => {
            const pool = PANEL_BGS[panel.getAttribute('data-tab')];
            if (!pool || pool.length < 2) return;

            const base = panel.querySelector('.panel-bg');
            if (!base) return;

            // capa gemela por encima: se cruza la opacidad y se intercambian papeles
            const fader = base.cloneNode(false);
            fader.classList.add('panel-bg-fade');
            fader.style.opacity = '0';
            base.parentNode.insertBefore(fader, base.nextSibling);

            let i = 0;
            let top = fader;      // capa que entra
            let bottom = base;    // capa visible

            setInterval(() => {
                i = (i + 1) % pool.length;
                const url = pool[i];
                const pre = new Image();
                pre.onload = () => {
                    top.style.backgroundImage = "url('" + url + "')";
                    top.style.opacity = '1';
                    setTimeout(() => {
                        bottom.style.opacity = '0';
                        const t = top; top = bottom; bottom = t;   // intercambio de papeles
                    }, 1400);
                };
                pre.src = url;
            }, 30000);
        });
    })();

    // --- AFFORDANCE DE LOS LOGOS DE PROYECTO ---
    // Una rejilla de logos en gris se lee como un muro de clientes y nadie la clica.
    // Se le anade a cada tarjeta un numero de orden, una flecha permanente y un
    // rotulo que aparece al pasar por encima. Se inyecta desde aqui para no repetir
    // el mismo marcado 21 veces en el HTML.
    document.querySelectorAll('.arch-logos').forEach(grid => {
        const cards = grid.querySelectorAll('.arch-logo');
        cards.forEach((card, i) => {
            if (card.querySelector('.arch-logo-go')) return;

            const num = document.createElement('span');
            num.className = 'arch-logo-num';
            num.textContent = String(i + 1).padStart(2, '0');
            card.prepend(num);

            const go = document.createElement('span');
            go.className = 'arch-logo-go';
            go.setAttribute('aria-hidden', 'true');
            go.innerHTML = '<i data-lucide="arrow-up-right"></i>';
            card.appendChild(go);

            const cta = document.createElement('span');
            cta.className = 'arch-logo-cta';
            cta.innerHTML = '<span class="lang-es">Ver proyecto</span>' +
                            '<span class="lang-en">View project</span>';
            card.appendChild(cta);
        });
    });

    // guino en la primera tarjeta la primera vez que se abre cada panel
    const hinted = {};
    function hintFirstLogo(panelId) {
        if (hinted[panelId]) return;
        hinted[panelId] = true;
        const first = document.querySelector('#' + panelId + ' .arch-logo');
        if (!first) return;
        first.classList.add('hint');
        setTimeout(() => first.classList.remove('hint'), 5000);
    }
    window.__hintFirstLogo = hintFirstLogo;

    // --- ARCHITECTURE: carretes horizontales de laminas (.mv-reel) ---
    document.querySelectorAll('.mv-reel').forEach(reel => {
        const track = reel.querySelector('.mv-reel-track');
        const slides = reel.querySelectorAll('.mv-reel-track > figure');
        if (!track || slides.length === 0) return;

        const dotsBox = reel.querySelector('.mv-reel-dots');
        const counter = reel.querySelector('.mv-reel-count');
        const prev = reel.querySelector('.mv-reel-arrow.prev');
        const next = reel.querySelector('.mv-reel-arrow.next');
        const dots = [];
        let idx = 0;

        function paint() {
            dots.forEach((d, i) => d.classList.toggle('active', i === idx));
            if (counter) {
                counter.textContent = String(idx + 1).padStart(2, '0') + ' / ' +
                                      String(slides.length).padStart(2, '0');
            }
            if (prev) prev.disabled = (idx === 0);
            if (next) next.disabled = (idx === slides.length - 1);
        }

        function goTo(i) {
            idx = Math.max(0, Math.min(slides.length - 1, i));
            track.scrollTo({ left: slides[idx].offsetLeft - track.offsetLeft, behavior: 'smooth' });
            paint();
        }

        if (dotsBox) {
            slides.forEach((_, i) => {
                const d = document.createElement('span');
                d.className = 'mv-reel-dot';
                d.addEventListener('click', () => goTo(i));
                dotsBox.appendChild(d);
                dots.push(d);
            });
        }
        if (prev) prev.addEventListener('click', () => goTo(idx - 1));
        if (next) next.addEventListener('click', () => goTo(idx + 1));

        // Pantalla completa: las laminas no pueden salirse de los margenes de la
        // pagina, asi que este es el sitio donde un plano se puede leer de verdad.
        const foot = reel.querySelector('.mv-reel-foot');
        if (foot && reel.requestFullscreen) {
            const fsBtn = document.createElement('button');
            fsBtn.type = 'button';
            fsBtn.className = 'mv-reel-full';
            fsBtn.innerHTML =
                '<i data-lucide="maximize"></i>' +
                '<span class="lang-es">Pantalla completa</span><span class="lang-en">Full screen</span>';
            foot.appendChild(fsBtn);

            fsBtn.addEventListener('click', () => {
                if (document.fullscreenElement === reel) {
                    document.exitFullscreen();
                } else {
                    reel.requestFullscreen().catch(() => {});
                }
            });

            // al entrar y salir cambia el ancho del carrete: hay que recolocar la lamina
            reel.addEventListener('fullscreenchange', () => {
                const full = document.fullscreenElement === reel;
                fsBtn.querySelectorAll('span').forEach(s => {
                    if (s.classList.contains('lang-es')) s.textContent = full ? 'Salir' : 'Pantalla completa';
                    if (s.classList.contains('lang-en')) s.textContent = full ? 'Exit' : 'Full screen';
                });
                requestAnimationFrame(() => {
                    track.scrollLeft = slides[idx].offsetLeft - track.offsetLeft;
                });
            });
        }

        // el indice real lo manda el scroll (tambien con gesto tactil o rueda)
        let settle;
        track.addEventListener('scroll', () => {
            clearTimeout(settle);
            settle = setTimeout(() => {
                const center = track.scrollLeft + track.clientWidth / 2;
                let best = 0, bestDist = Infinity;
                slides.forEach((s, i) => {
                    const dist = Math.abs((s.offsetLeft - track.offsetLeft + s.offsetWidth / 2) - center);
                    if (dist < bestDist) { bestDist = dist; best = i; }
                });
                idx = best;
                paint();
            }, 90);
        }, { passive: true });

        paint();
    });

    // los botones de pantalla completa se han creado ahora: pintar sus iconos
    if (window.lucide) window.lucide.createIcons();

    // --- VIDEOS: parar el que no se esta viendo ---
    // Un video en autoplay seguia sonando y consumiendo al salir de pantalla
    // (o al cambiar de pestana del sitio). Solo se reproduce lo que se ve.
    if ('IntersectionObserver' in window) {
        const videoObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const v = entry.target;
                if (entry.isIntersecting) {
                    // respetar una pausa hecha a mano por el usuario
                    if (v.dataset.userPaused !== '1') v.play().catch(() => {});
                } else if (!v.paused) {
                    v.dataset.autoPaused = '1';
                    v.pause();
                }
            });
        }, { threshold: 0.25 });

        document.querySelectorAll('video').forEach(v => {
            v.addEventListener('pause', () => {
                if (v.dataset.autoPaused === '1') { v.dataset.autoPaused = ''; return; }
                v.dataset.userPaused = '1';
            });
            v.addEventListener('play', () => { v.dataset.userPaused = ''; });
            videoObserver.observe(v);
        });
    }

    // --- SCROLL COLLAPSE STATE FOR HEADER ---
    const header = document.querySelector('header');
    
    function checkHeaderScroll() {
        if (!mainWrapperSite.classList.contains('hidden')) {
            if (window.scrollY > 80) {
                header.classList.add('scrolled');
            } else {
                header.classList.remove('scrolled');
            }
        } else {
            header.classList.remove('scrolled');
        }
    }

    window.addEventListener('scroll', checkHeaderScroll);
    
    // Trigger scroll check on transitions
    selectorPanels.forEach(panel => {
        panel.addEventListener('click', () => {
            setTimeout(checkHeaderScroll, 100);
        });
    });
    
    if (logoBack) {
        logoBack.addEventListener('click', () => {
            setTimeout(checkHeaderScroll, 100);
        });
    }

    // --- PROJECTS NAVIGATION STRIP SMOOTH SCROLL ---
    const navStripItems = document.querySelectorAll('.nav-strip-item, .logo-band-item, .arch-logo[data-scroll-to]');
    navStripItems.forEach(item => {
        item.addEventListener('click', (e) => {
            const targetProj = item.getAttribute('data-scroll-to');
            const targetBlock = document.querySelector(`.project-section-block[data-project="${targetProj}"]`);
            if (targetBlock) {
                const rect = targetBlock.getBoundingClientRect();
                const absoluteTop = rect.top + window.scrollY;
                window.scrollTo({
                    top: absoluteTop,
                    behavior: 'smooth'
                });
            }
        });
    });
});
