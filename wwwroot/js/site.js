document.addEventListener('DOMContentLoaded', function () {
    var btnIn = document.getElementById('btn-in');
    var btnOut = document.getElementById('btn-out');
    var btnStats = document.getElementById('btn-stats');
    var tagInput = document.getElementById('tagNumber');
    var messageBox = document.getElementById('message-box');
    var modalOverlay = document.getElementById('modal-overlay');
    var modalClose = document.getElementById('modal-close');
    var modalBody = document.getElementById('modal-body');
    var areaBContent = document.getElementById('area-b-content');

    function showMessage(text, isError) {
        messageBox.textContent = text;
        messageBox.className = 'message-box ' + (isError ? 'error' : 'success');
        messageBox.style.display = 'block';
    }

    function hideMessage() {
        messageBox.style.display = 'none';
    }

    function postRequest(url, tagNumber, callback) {
        var xhr = new XMLHttpRequest();
        xhr.open('POST', url, true);
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.onreadystatechange = function () {
            if (xhr.readyState === 4) {
                if (xhr.status === 200) {
                    callback(JSON.parse(xhr.responseText));
                } else {
                    showMessage('An error occurred. Please try again.', true);
                }
            }
        };
        xhr.send(JSON.stringify({ tagNumber: tagNumber }));
    }

    btnIn.addEventListener('click', function () {
        var tag = tagInput.value.trim();
        if (!tag) {
            showMessage('Please enter a tag number.', true);
            return;
        }
        hideMessage();
        postRequest('/Home/CheckIn', tag, function (data) {
            showMessage(data.message, !data.success);
            if (data.success) {
                areaBContent.innerHTML = data.areaBHtml;
                tagInput.value = '';
            }
        });
    });

    btnOut.addEventListener('click', function () {
        var tag = tagInput.value.trim();
        if (!tag) {
            showMessage('Please enter a tag number.', true);
            return;
        }
        hideMessage();
        postRequest('/Home/CheckOut', tag, function (data) {
            showMessage(data.message, !data.success);
            if (data.success) {
                areaBContent.innerHTML = data.areaBHtml;
                tagInput.value = '';
            }
        });
    });

    btnStats.addEventListener('click', function () {
        modalBody.textContent = 'Loading...';
        modalOverlay.style.display = 'flex';

        var xhr = new XMLHttpRequest();
        xhr.open('GET', '/Home/Stats', true);
        xhr.onreadystatechange = function () {
            if (xhr.readyState === 4 && xhr.status === 200) {
                var stats = JSON.parse(xhr.responseText);
                modalBody.innerHTML =
                    '<p><span class="stat-label">Available spots:</span> ' + stats.availableSpots + '</p>' +
                    '<p><span class="stat-label">Today\'s revenue:</span> $' + stats.todayRevenue.toFixed(2) + '</p>' +
                    '<p><span class="stat-label">Avg cars/day (30 days):</span> ' + stats.avgCarsPerDay + '</p>' +
                    '<p><span class="stat-label">Avg revenue/day (30 days):</span> $' + stats.avgRevenuePerDay.toFixed(2) + '</p>';
            }
        };
        xhr.send();
    });

    modalClose.addEventListener('click', function () {
        modalOverlay.style.display = 'none';
    });

    modalOverlay.addEventListener('click', function (e) {
        if (e.target === modalOverlay) {
            modalOverlay.style.display = 'none';
        }
    });

    tagInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            btnIn.click();
        }
    });
});
