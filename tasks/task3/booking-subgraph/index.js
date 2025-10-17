import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';
import grpc from '@grpc/grpc-js';
import protoLoader from '@grpc/proto-loader';

const typeDefs = gql`
  extend schema
    @link(url: "https://specs.apollo.dev/federation/v2.0", import: ["@key"])

  type Booking @key(fields: "id") {
    id: ID!
    userId: String!
    hotelId: String!
    promoCode: String
    discountPercent: Int
    status: String
    price: Float
    createdAt: String
    hotel: Hotel
  }

  type Query {
    bookingsByUser(userId: String!): [Booking]
  }

  type Hotel @key(fields: "id") {
    id: ID!
  }
`;

// Загрузка proto для gRPC (Protos/booking.proto из task2; скопируйте в subgraph)
const packageDef = protoLoader.loadSync('./Protos/booking.proto');
const grpcObj = grpc.loadPackageDefinition(packageDef);
const client = new grpcObj.booking.BookingService(
  'booking-service:8080',
  grpc.credentials.createInsecure()
);

const resolvers = {
  Query: {
    bookingsByUser: async (_, { userId }, { req }) => {
      // // ACL: Проверка header userid
      const userid = req.headers['userid'];
      if (!userid) throw new Error('Unauthorized: userid header required');
      // const headerUser = req.headers['userid']; // ← lowercase!
      // if (!headerUser) {
      //   console.error(' Missing userid header ' + req.headers);
      //   throw new Error('Unauthorized: userid header is required ' + req.headers);
      // }
      // if (headerUser !== userId) {
      //   console.warn(` ACL DENIED: ${headerUser} tried to access ${userId}`);
      //   return []; // или throw new Error('Forbidden')
      // }
      return new Promise((resolve, reject) => {
        client.ListBookings({ user_id: userId }, (err, response) => {
          if (err) reject(err);
          // Mapping: Добавьте status если в response нет
          const bookings = (response.bookings || []).map(b => ({ ...b, status: b.status || 'pending' }));
          resolve(bookings);
        });
      });
    },
  },
  Booking: {
    __resolveReference: async (booking) => {
      // Federation resolver для Booking by ID (если query по ID)
      // const client = new bookingProto.BookingService('booking-service:8080', grpc.credentials.createInsecure());
      return new Promise((resolve, reject) => {
        client.GetBooking({ id: booking.id }, (err, response) => {  // Добавьте GetBooking в proto если нужно
          if (err) reject(err);
          resolve(response || null);
        });
      });
    },
    hotel: (booking) => ({ __typename: 'Hotel', id: booking.hotelId }),
  },
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4001 },
  context: async ({ req }) => ({ req }),
}).then(() => {
  console.log('✅ Booking subgraph ready at http://localhost:4001/');
});
