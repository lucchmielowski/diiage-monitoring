FROM node:18

WORKDIR /usr/src/app

COPY *.ts .
COPY tsconfig.json .
COPY package.json .

RUN npm i

CMD ["npx", "ts-node", "--require", "./instrumentation.ts", "app.ts"]