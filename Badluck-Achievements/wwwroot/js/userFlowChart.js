function renderAchievementsFlowChart(completed, uncompleted) {
	const canvas = document.getElementById('achievementChart');
	const ctx = canvas.getContext('2d');

	new Chart(ctx, {
		type: 'doughnut',
		data: {
			labels: ['Completed', 'Uncompleted'],
			datasets: [{
				data: [completed, uncompleted],
				backgroundColor: ['#4CAF50', '#F44336'],
				borderWidth: 0
			}]
		},
		options: {
			responsive: false,
			maintainAspectRatio: false,
			cutout: '70%',
			plugins: { legend: { display: false } }
		}
	});
}

function renderGamesFlowChart(completed, inProgress, uncompleted) {
	const canvas = document.getElementById('gamesChart');
	const ctx = canvas.getContext('2d');

	new Chart(ctx, {
		type: 'doughnut',
		data: {
			labels: ['Done', 'Ongoing', 'Not Started'],
			datasets: [{
				data: [completed, inProgress, uncompleted],
				backgroundColor: ['#6E62D6', '#06B6D4', '#9E9E9E'],
				borderWidth: 0
			}]
		},
		options: {
			responsive: false,
			maintainAspectRatio: false,
			cutout: '70%',
			plugins: { legend: { display: false } }
		}
	});
}