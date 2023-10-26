import express, { Express } from 'express'
import logger from './loggger'

const PORT: number = parseInt(process.env.PORT || '3001 ')
const app: Express = express()

function getRandomNumber(min: number, max: number) {
  logger.info('Generating random number')
  return Math.floor(Math.random() * (max - min) + min)
}

app.get('/roll', (req, res) => {
  logger.info('calling getRandomNumber(1,6) from /roll')
  res.send(getRandomNumber(1, 6).toString())
})

app.listen(PORT, () => {
  logger.info(`Listenning to requests on http://localhost:${PORT}`)
})