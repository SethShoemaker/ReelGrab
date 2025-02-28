export function formatSeasonEpisodeDigit(number: number|null): string {
    if (number === null || number === undefined) return '';
    return number < 10 ? `0${number}` : number.toString();
}

export function formatSeasonEpisodeNumber(seasonNumber: number, episodeNumber: number): string {
    return `S${formatSeasonEpisodeDigit(seasonNumber)}E${formatSeasonEpisodeDigit(episodeNumber)}`
}

export function formatSeasonEpisode(seasonNumber: number, episodeNumber: number, episodeTitle: string): string {
    return `${formatSeasonEpisodeNumber(seasonNumber, episodeNumber)} ${episodeTitle}`;
}