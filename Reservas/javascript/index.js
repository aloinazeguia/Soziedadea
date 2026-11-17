import { AlertMessage } from "../../components/AlertComponents.js";

document.querySelector("#FechaReserva").addEventListener("submit", function(event) {
    event.preventDefault();
    const numComensales = document.getElementById("personas").value;
    const fecha = document.getElementById("fecha").value;
    //const hora = document.getElementById("hora").value;
    localStorage.setItem("personas",numComensales);
    localStorage.setItem("fecha", fecha);
    //localStorage.setItem("hora",hora);
  
    const url = `http://localhost:3000/GetMesasLibresFecha?numComensales=${encodeURIComponent(numComensales)}&fecha=${encodeURIComponent(fecha)}`;

    fetch(url, {
      method: "GET"
    })
    .then(response => response.json())
    .then(data => {
      if (data.mesas.length!=0) {
        //alert(`✅ Lekua dago ${fecha} egunean ${personas} lagunentzat.`);
        const numMesa=data.mesas[0].numMesa;
        localStorage.setItem("numMesa",numMesa);
        window.location.href = '/Reservas/html/iconfirmacion.html';
        
      } else if (data.length== 0) {
        let message=`❌ Ez dago lekurik ${fecha} egunerako`;
        AlertMessage(message);

      } else {
        alert("⚠️ " + (data.error || "Errorea erabilgarritasuna egiaztatzean."));
      }
    })
   .catch(error => {
      console.error("Error:", error);
      alert("❌ Errorea zerbitzariarekin konektatzean.");
    
    });
  });