import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';
import fetch from 'node-fetch';

const typeDefs = gql`
  extend schema
    @link(url: "https://specs.apollo.dev/federation/v2.0",
          import: ["@key", "@shareable"])

  type Hotel @key(fields: "id") {
    id: ID!
    name: String
    city: String
    stars: Int
  }

  type Query {
    hotelsByIds(ids: [ID!]!): [Hotel]
  }
`;

const resolvers = {
  Hotel: {
    __resolveReference: async ({ id }) => {
      try {
        const userid = req.headers['userid'];
        if (!userid) throw new Error('Unauthorized: userid header required');

        const res = await fetch(`http://hotelio-monolith:8080/api/hotels/${id}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        return { // Mapping: monolith fields → schema
          __typename: 'Hotel',
          id: data.id || id,
          name: data.description || 'Test Hotel Moscow',  // Ваш INSERT
          city: data.city || 'Moscow',
          stars: Math.floor(data.rating) || 4
        };
      } catch (err) {
        console.error(`Failed to fetch hotel ${id}:`, err.message);
        return { __typename: 'Hotel', id, name: "Unknown", city: "N/A", stars: 0 };
      }
    },
  },
  Query: {
    hotelsByIds: async (_, { ids }, { req }) => {
      const userid = req.headers['userid'];
      if (!userid) throw new Error('Unauthorized: userid header required');

      const hotels = [];
      for (const id of ids) {
        const response = await fetch(`http://hotelio-monolith:8080/api/hotels/${id}`);
        if (response.ok) hotels.push(await response.json());
      }
      return hotels;
    },
  },
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4002 },
}).then(() => {
  console.log('✅ Hotel subgraph ready at http://localhost:4002/');
});
