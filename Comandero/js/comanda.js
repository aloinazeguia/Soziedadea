document.addEventListener('DOMContentLoaded', () => {
    let comandasPendientes = JSON.parse(localStorage.getItem('comandasPendientes')) || [];

    const empresaDiv = document.querySelector('.empresa');
    const datosEmpresaDiv = document.querySelector('.datosEmpresa');
    const listaCobro = document.getElementById('listaCobro');

    if (comandasPendientes.length === 0) {
        listaCobro.innerHTML = '<li>Ez dago kobratzeko eskaerarik.</li>';
        return;
    }

    listaCobro.innerHTML = '';
    comandasPendientes.forEach((comanda, index) => {
        const li = document.createElement('li');
        li.style.cursor = 'pointer';
        li.textContent = `Eskaera #${comanda.id} - Mahaia: ${comanda.mesaId}`;
        li.addEventListener('click', () => mostrarDetalleComanda(index));
        listaCobro.appendChild(li);
    });

    function mostrarDetalleComanda(indice) {
        const comanda = comandasPendientes[indice];

        empresaDiv.textContent = comanda.sala || 'ARETO EZEZAGUNA';
        datosEmpresaDiv.textContent = `BEZEROAK: ${comanda.comensales || 0}`;

        listaCobro.innerHTML = '';

        let total = 0;
        comanda.pedido.forEach(producto => {
            const item = document.createElement('li');
            const subtotal = producto.precio * producto.cantidad;
            item.textContent = `${producto.nombre} x${producto.cantidad} - ${subtotal.toFixed(2)}€`;
            listaCobro.appendChild(item);
            total += subtotal;
        });

        const totalItem = document.createElement(`li`);
        totalItem.style.fontWeight = 'bold';
        totalItem.textContent = `GUZTIRA: ${total.toFixed(2)}€`;
        listaCobro.appendChild(totalItem);

        const botonCobrar = document.createElement('button');
        botonCobrar.textContent = 'Eskaera kobratu';
        botonCobrar.addEventListener('click', () => {
            comandasPendientes.aplice(indice, 1);
            localStorage.setItem('comandasPendientes', JSON.stringify(comandasPendientes));
            location.reload();
        });
        listaCobro.appendChild(botonCobrar);
    }
});