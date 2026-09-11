import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';
import { DIA_CHI_GOC } from './DichVuApi';

export function TaoKetNoiChat(token: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${DIA_CHI_GOC}/hub/chat`, { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}
