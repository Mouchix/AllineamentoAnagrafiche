const Notifica = {
    success: (messaggio, callback) => {
        Swal.fire({
            title: 'Operazione riuscita',
            text: messaggio,
            icon: 'success',
            confirmButtonColor: '#158CBA'
        }).then(() => { if (callback) callback(); });
    },
    error: (messaggio) => {
        Swal.fire({
            title: 'Errore',
            text: messaggio,
            icon: 'error',
            confirmButtonColor: '#158CBA'
        });
    }
};