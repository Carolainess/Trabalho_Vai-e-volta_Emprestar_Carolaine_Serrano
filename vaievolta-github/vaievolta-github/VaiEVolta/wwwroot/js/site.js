// VAI E VOLTA — pequenos comportamentos da interface

// Avisos somem sozinhos depois de alguns segundos
document.querySelectorAll('.aviso').forEach(aviso => {
    setTimeout(() => {
        aviso.classList.add('saindo');
        setTimeout(() => aviso.remove(), 300);
    }, 4500);
    aviso.addEventListener('click', () => aviso.remove());
});

// Elementos com .revelar aparecem suavemente ao rolar a página
const observador = new IntersectionObserver(entradas => {
    entradas.forEach(e => {
        if (e.isIntersecting) {
            e.target.classList.add('visivel');
            observador.unobserve(e.target);
        }
    });
}, { threshold: .12 });
document.querySelectorAll('.revelar').forEach((el, i) => {
    el.style.transitionDelay = `${(i % 4) * 70}ms`;
    observador.observe(el);
});

// Pré-visualização da foto antes de enviar
document.querySelectorAll('[data-preview]').forEach(input => {
    input.addEventListener('change', () => {
        const arquivo = input.files[0];
        const img = document.querySelector(input.dataset.preview);
        if (arquivo && img) {
            img.src = URL.createObjectURL(arquivo);
            img.classList.remove('d-none');
            document.querySelectorAll('[data-sem-foto]').forEach(el => el.classList.add('d-none'));
        }
    });
});

// Pede confirmação antes de ações importantes (excluir, recusar...)
document.querySelectorAll('form[data-confirmar]').forEach(form => {
    form.addEventListener('submit', e => {
        if (!confirm(form.dataset.confirmar)) e.preventDefault();
    });
});

// Foto quebrada vira um ícone bonito em vez de imagem quebrada
document.querySelectorAll('img[data-fallback]').forEach(img => {
    img.addEventListener('error', () => { img.src = '/img/sem-foto.svg'; }, { once: true });
});
